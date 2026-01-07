#!/bin/bash

set -euo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================

readonly DEPLOY_DIR="${HOME_DIR}/production"
readonly LOG_DIR="/var/log/lexiq/deployment"
readonly LOG_FILE="${LOG_DIR}/deploy-$(date +%Y%m%d-%H%M%S).log"
readonly ENVIRONMENT_FILE="/tmp/.deploy.env"

# Exit codes
readonly EXIT_SUCCESS=0
readonly EXIT_SYSTEM_UPDATE_FAILED=1
readonly EXIT_FILE_NOT_FOUND=1
readonly EXIT_GIT_FAILED=2
readonly EXIT_DOCKER_PULL_FAILED=3
readonly EXIT_DOCKER_START_FAILED=4

# ============================================================================
# GITHUB ACTIONS LOGGING HELPERS
# ============================================================================

log() {
  local level="$1"      # First argument: "error", "warning", or "notice"
  shift                 # Remove first argument, leaving only the message parts
  local message="$*"    # Combine all remaining arguments into one string
  local timestamp="$(date +'%Y-%m-%d %H:%M:%S')"
  
  echo "[${timestamp}] ${message}" | tee -a "$LOG_FILE"
  
  # Send GitHub Actions annotations
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

  if [ -f "$ENVIRONMENT_FILE" ]; then
    . "$ENVIRONMENT_FILE"
    log_info "Loaded environment variables from $ENVIRONMENT_FILE"
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
  
  # Show last 20 lines of log
  if [ -f "$LOG_FILE" ]; then
    echo "::group::Last 20 log lines"
    tail -20 "$LOG_FILE"
    echo "::endgroup::"
  fi
  
  exit "$exit_code"
}

trap trap_error ERR

# ============================================================================
# INITIALIZATION
# ============================================================================

copy_docker_compose_file() {
  start_group "Copying Docker Compose File"
  local source_file="${DEPLOY_DIR}/docker-compose.prod.yml"
  local target_file="${DEPLOY_DIR}/docker-compose.yml"

  if [ -f "$source_file" ]; then
    mv "$source_file" "$target_file"
    log_info "Renamed $source_file to $target_file"
  elif [ -f "$target_file" ]; then
    log_info "File $target_file already exists. No rename needed."
  else
    log_warning "Source file not found: $source_file"
  fi

  end_group
}

initialize() {
  start_group "Initialization"
  
  log_info "Branch: ${BRANCH}"
  log_info "Commit: ${COMMIT}"
  log_info "Triggered by: ${ACTOR}"
  log_info "Event: ${EVENT}"
  
  # Create log directory
  if [ ! -d "$LOG_DIR" ]; then
    sudo mkdir -p "$LOG_DIR"
    log_info "Created log directory: ${LOG_DIR}"
  fi
  
  log_info "Log file: ${LOG_FILE}"

  copy_docker_compose_file

  end_group
}

# ============================================================================
# SYSTEM OPERATIONS
# ============================================================================

update_system() {
  start_group "System Updates"
  
  log_info "Updating system packages..."
  if sudo apt update 2>&1 | tee -a "$LOG_FILE"; then
    log_success "System package list updated"
  else
    log_error "System update failed"
    end_group
    exit $EXIT_SYSTEM_UPDATE_FAILED
  fi
  
  log_info "Upgrading system packages..."
  if sudo apt upgrade -y 2>&1 | tee -a "$LOG_FILE"; then
    log_success "System packages upgraded"
  else
    log_warning "System upgrade had issues (non-critical)"
  fi
  
  end_group
}

install_dependencies() {
  start_group "Installing Dependencies"
  
  echo "Checking docker installation"
  if command -v docker &> /dev/null; then
      log_info "Docker is already installed"
  else
      log_error "Docker is not installed"
      end_group
      exit $EXIT_SYSTEM_UPDATE_FAILED
  fi   

  end_group
}

# ============================================================================
# DOCKER OPERATIONS
# ============================================================================

pull_docker_images() {
  start_group "Pulling Docker Images"
  
  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_PULL_FAILED
  
  log_info "Pulling latest Docker images..."
  if sudo docker compose pull 2>&1 | tee -a "$LOG_FILE"; then
    log_success "Docker images pulled"
  else
    log_error "Docker pull failed"
    end_group
    exit $EXIT_DOCKER_PULL_FAILED
  fi
  
  end_group
}

stop_containers() {
  start_group "Stopping Containers"
  
  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_START_FAILED
  
  log_info "Stopping existing containers..."
  if sudo docker compose down 2>&1 | tee -a "$LOG_FILE"; then
    log_success "Containers stopped"
  else
    log_warning "Issue stopping containers"
  fi
  
  end_group
}

build_containers() {
  start_group "Building Containers"
  
  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_START_FAILED
  
  if sudo docker compose build 2>&1 | tee -a "$LOG_FILE"; then
      log_success "Build successful"
  else
    log_error "Build failed"
    end_group
    exit $EXIT_DOCKER_START_FAILED
  fi
  
  end_group
}

start_containers() {
  start_group "Starting Containers"
  
  cd "$DEPLOY_DIR" || exit $EXIT_DOCKER_START_FAILED
  
  log_info "Starting containers..."
  if sudo docker compose up -d 2>&1 | tee -a "$LOG_FILE"; then
    log_success "Containers started"
  else
    log_error "Failed to start containers"
    
    echo "::group::Container Logs"
    sudo docker compose logs --tail=100 2>&1 | tee -a "$LOG_FILE"
    echo "::endgroup::"
    
    end_group
    exit $EXIT_DOCKER_START_FAILED
  fi
  
  end_group
}

cleanup_docker() {
  start_group "Docker Cleanup"
  
  log_info "Cleaning up unused Docker images..."
  if sudo docker image prune -f 2>&1 | tee -a "$LOG_FILE"; then
    log_success "Docker cleanup complete"
  else
    log_warning "Docker cleanup had issues"
  fi
  
  end_group
}

show_container_status() {
  start_group "Container Status"
  
  cd "$DEPLOY_DIR" || return
  
  log_info "Current container status:"
  sudo docker compose ps 2>&1 | tee -a "$LOG_FILE"
  
  end_group
}

# ============================================================================
# HEALTH CHECKS
# ============================================================================

verify_deployment() {
  start_group "Deployment Verification"
  
  cd "$DEPLOY_DIR" || return
  
  log_info "Checking container health..."
  
  local healthy=true
  local containers=$(sudo docker compose ps -a --format json | jq -r '.Name')
  
  for container in $containers; do
    local state=$(sudo docker inspect --format='{{.State.Status}}' "$container" 2>/dev/null || echo "not found")
    
    if [ "$state" == "running" ]; then
      log_success "Container ${container} is running"
    else
      log_error "Container ${container} is ${state}"
      healthy=false
    fi
  done
  
  if [ "$healthy" = true ]; then
    log_success "All containers are healthy"
  else
    log_error "Some containers are unhealthy"
  fi
  
  end_group
}

verify_deployment() {
  start_group "Deployment Verification"
  
  # Ensure DEPLOY_DIR is valid
  if [ -z "$DEPLOY_DIR" ] || [ ! -d "$DEPLOY_DIR" ]; then
    log_error "Deployment directory $DEPLOY_DIR not found"
    return 1
  fi

  cd "$DEPLOY_DIR" || return
  
  log_info "Checking container health..."
  
  local healthy=true
  
  local container_ids=$(sudo docker compose ps -q)
  
  if [ -z "$container_ids" ]; then
    log_error "No containers found for this project."
    end_group
    return 1
  fi

  for id in $container_ids; do
    # Get Name, Status, and Health in one go
    local info=$(sudo docker inspect --format='{{.Name}}|{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' "$id")
    
    local name=$(echo "$info" | cut -d'|' -f1 | sed 's/\///')
    local status=$(echo "$info" | cut -d'|' -f2)
    local health=$(echo "$info" | cut -d'|' -f3)
    
    if [ "$status" = "running" ]; then
      if [ "$health" = "unhealthy" ]; then
        log_error "Container $name is RUNNING but UNHEALTHY"
        healthy=false
      else
        log_success "Container $name is $status ($health)"
      fi
    else
      log_error "Container $name is NOT running (State: $status)"
      healthy=false
    fi
  done
  
  if [ "$healthy" = true ]; then
    log_success "All containers are healthy"
  else
    log_error "Some containers failed health checks"
    exit 1
  fi
  
  end_group
}

# ============================================================================
# MAIN DEPLOYMENT FLOW
# ============================================================================

main() {
  local start_time=$(date +%s)

  load_env

  initialize
  
  update_system
  install_dependencies
  
  pull_docker_images
  stop_containers
  build_containers
  start_containers
  
  cleanup_docker
  show_container_status
  verify_deployment
  
  local end_time=$(date +%s)
  local duration=$((end_time - start_time))
  
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