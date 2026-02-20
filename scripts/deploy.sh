#!/bin/bash

set -eEuo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================
readonly ENVIRONMENT_FILE="/tmp/.deploy.env"
LOG_DIR="/var/log/lexiq/deployment"
LOG_FILE="${LOG_DIR}/deploy-$(date +%Y%m%d-%H%M%S).log"  

# Exit codes
readonly EXIT_SUCCESS=0
readonly EXIT_FILE_NOT_FOUND=1
readonly EXIT_DOCKER_PULL_FAILED=3
readonly EXIT_DOCKER_AUTH_FAILED=3
readonly EXIT_DOCKER_START_FAILED=4

# ============================================================================
# SECURITY & REDACTION HELPERS
# ============================================================================

mask_ips() {
  # Replaces standard IPv4 addresses with [REDACTED_IP]
  sed -E 's/\b([0-9]{1,3}\.){3}[0-9]{1,3}\b/[REDACTED_IP]/g'
}

# ============================================================================
# GITHUB ACTIONS LOGGING HELPERS
# ============================================================================

log() {
  local level="$1"      # First argument: "error", "warning", or "notice"
  shift                 # Remove first argument, leaving only the message parts
  
  # Mask IPs in the message itself before processing
  local message
  message=$(echo "$*" | mask_ips)
  
  local timestamp
  timestamp=$(date +"%Y-%m-%d %H:%M:%S")
  
  echo "[${timestamp}] ${message}" | tee -a "$LOG_FILE"

  case "$level" in
    error)
      echo "::error::${message}"
      ;;
    warning)
      echo "::warning::${message}"
      ;;
    notice)
      echo "::notice::${message}"
      ;;
  esac
}

log_info() {
  log "info" "$@" # Preserves spaces/arguments separately
}

log_error() {
  log "error" "$@"
}

log_warning() {
  log "warning" "$@"
}

log_success() {
  log "notice" "✅ $*"
}

start_group() {
  echo "::group::$1"
  log_info "===== $1 ====="
}

end_group() {
  echo "::endgroup::"
}

# ============================================================================
# LOAD ENVIRONMENT VARIABLES
# ============================================================================

load_env() {
  start_group "Loading Environment Variables"

  if [ ! -d "$LOG_DIR" ]; then
    sudo mkdir -p "$LOG_DIR"
    log_info "Created log directory: ${LOG_DIR}"
  fi

  if [ -f "$ENVIRONMENT_FILE" ]; then
    . "$ENVIRONMENT_FILE"
    log_info "Loaded environment variables from $ENVIRONMENT_FILE"

    export REGISTRY
    export REPO_LOWER
    export BRANCH
    export EVENT

    log_info "REGISTRY=${REGISTRY}"
    log_info "REPO_LOWER=${REPO_LOWER}"
    log_info "BRANCH=${BRANCH}"
    log_info "Docker image will be: ${REGISTRY}/${REPO_LOWER}-backend:${BRANCH}"
    
  else
    log_error "Environment file $ENVIRONMENT_FILE not found"
    exit $EXIT_FILE_NOT_FOUND
  fi

  end_group
}

# ============================================================================
# ERROR HANDLING
# ============================================================================

trap_error() {
  local exit_code=$?
  local line_number=${BASH_LINENO[0]:-?}
  local command=${BASH_COMMAND:-}
  
  log_error "Command failed at line ${line_number} with exit code ${exit_code}"
  log_error "Failed command: ${command}"
  
  # Show last 20 lines of log with a final redaction pass
  if [ -f "$LOG_FILE" ]; then
    echo "::group::Last 20 log lines"
    tail -20 "$LOG_FILE" | mask_ips
    echo "::endgroup::"
  fi
  
  exit "$exit_code"
}

trap trap_error ERR

# ============================================================================
# INITIALIZATION
# ============================================================================

initialize() {
  start_group "Initialization"
  
  log_info "Branch: ${BRANCH}"
  log_info "Commit: ${COMMIT}"
  log_info "Triggered by: ${ACTOR}"
  log_info "Event: ${EVENT}"
  
  log_info "Log file: ${LOG_FILE}"

  end_group
}

# ============================================================================
# SYSTEM OPERATIONS
# ============================================================================

install_dependencies() {
  start_group "Installing Dependencies"
  
  echo "Checking docker installation"
  if command -v docker &> /dev/null; then
      log_info "Docker is already installed"
  else
      log_error "Docker is not installed"
      end_group
      exit $EXIT_FILE_NOT_FOUND
  fi   

  end_group
}

# ============================================================================
# DOCKER OPERATIONS
# ============================================================================

run_docker_login() {
  echo "$DOCKER_PASSWORD" | docker login "${REGISTRY}" --username "$DOCKER_USERNAME" --password-stdin > /dev/null 2>&1
}

authenticate_docker_registry() {
  start_group "Docker Registry Authentication"
  
  if [ -n "${DOCKER_USERNAME:-}" ] && [ -n "${DOCKER_PASSWORD:-}" ]; then
    log_info "Authenticating to Docker registry as ${DOCKER_USERNAME}..."
    if run_docker_login; then
      log_success "Docker registry authentication successful"
    else
      log_error "Docker registry authentication failed"
      end_group
      exit $EXIT_DOCKER_AUTH_FAILED
    fi
  else
    log_warning "Docker registry credentials not provided; skipping authentication"
  fi
  
  end_group
}

deploy_containers() {
  start_group "Deploying Containers"

  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_PULL_FAILED

  log_info "Pulling latest app images..."
  if docker compose pull 2>&1 | mask_ips | tee -a "$LOG_FILE"; then
    log_success "Docker images pulled"
  else
    log_error "Docker pull failed"
    end_group
    exit $EXIT_DOCKER_PULL_FAILED
  fi

  log_info "Starting containers..."
  if docker compose up -d --wait 2>&1 | mask_ips | tee -a "$LOG_FILE"; then
    log_success "Containers started"
  else
    log_error "Failed to start containers"

    echo "::group::Container Logs"
    docker compose logs --tail=100 2>&1 | mask_ips | tee -a "$LOG_FILE"
    echo "::endgroup::"

    end_group
    exit $EXIT_DOCKER_START_FAILED
  fi

  end_group
}

# ============================================================================
# CERTIFICATE MANAGEMENT
# ============================================================================

maybe_init_letsencrypt() {
  start_group "Let's Encrypt Certificate Check"

  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_START_FAILED

  # Find the volume by suffix to avoid hardcoding the compose project prefix
  local vol_name
  vol_name=$(docker volume ls --format '{{.Name}}' | grep 'production_letsencrypt-certs' | head -1 || true)

  local certs_exist=false

  if [ -n "$vol_name" ]; then
      local mountpoint
      mountpoint=$(docker volume inspect "$vol_name" --format '{{.Mountpoint}}')
      log_info "Found existing Let's Encrypt volume: ${vol_name} at ${mountpoint}"

      if [ -f "${mountpoint}/live/lexiqlanguage.eu/fullchain.pem" ]; then
          log_info "Certificates already exist in ${vol_name} — skipping initialization"
          certs_exist=true
      else
          log_warning "Volume exists, but certificates are missing in ${mountpoint}"
      fi
  fi

  if [ "$certs_exist" = false ]; then
      log_info "Initializing certificates — running init-letsencrypt.sh..."
      bash "${DEPLOY_DIR}/scripts/init-letsencrypt.sh" 2>&1 | mask_ips | tee -a "$LOG_FILE"
      log_success "Certificate initialization complete"
  else
      end_group
      return 0
  fi

  end_group
}

# ============================================================================
# MAIN DEPLOYMENT FLOW
# ============================================================================

main() {
  local start_time
  start_time=$(date +%s)

  load_env

  initialize
  
  export TAG="${BRANCH}"
  
  install_dependencies

  authenticate_docker_registry
  maybe_init_letsencrypt
  deploy_containers

  local end_time
  end_time=$(date +%s)
  local duration
  duration=$((end_time - start_time))
  
  start_group "Deployment Summary"
  log_success "Deployment completed successfully"
  log_info "Duration: ${duration} seconds"
  log_info "Log file: ${LOG_FILE}"
  end_group
  
  exit $EXIT_SUCCESS
}

# ============================================================================
# SCRIPT ENTRY POINT
# ============================================================================

main "$@"