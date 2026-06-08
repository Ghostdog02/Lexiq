#!/bin/bash

set -eEuo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================
LOG_DIR="/var/log/lexiq/deployment"
LOG_FILE="${LOG_DIR}/deploy-$(date +%Y%m%d-%H%M%S).log"

# Exit codes
readonly EXIT_SUCCESS=0
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

  mkdir -p "$LOG_DIR"

  log_info "Branch: ${BRANCH}"
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
      exit 1
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
# MAIN DEPLOYMENT FLOW
# ============================================================================

main() {
  local start_time
  start_time=$(date +%s)

  local server_env="$HOME/production/.env"
  if [ -f "$server_env" ]; then
    . "$server_env"
  fi

  initialize
  
  export TAG="${BRANCH}"
  
  install_dependencies

  authenticate_docker_registry
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