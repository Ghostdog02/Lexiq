#!/bin/sh

set -eEuo pipefail

# ============================================================================
# CONFIGURATION
# ============================================================================
LOG_DIR="/var/log/lexiq/deployment"
LOG_FILE="${LOG_DIR}/deploy-$(date +%Y%m%d-%H%M%S).log"  

# ============================================================================
# GITHUB ACTIONS LOGGING HELPERS
# ============================================================================

log() {
  local level="$1"      # First argument: "error", "warning", or "notice"
  shift                 # Remove first argument, leaving only the message parts
  local message="$*"    # Combine all remaining arguments into one string
  local timestamp
  timestamp=$(date +"%Y-%m-%d %H:%M:%S")
  
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
# CONTAINER STATUS
# ============================================================================

show_container_status() {
  start_group "Container Status"
  
  cd "$DEPLOY_DIR" || return
  
  log_info "Current container status:"
  docker compose ps -a 2>&1 | tee -a "$LOG_FILE"
  
  end_group
}

# ============================================================================
# HEALTH CHECKS
# ============================================================================

verify_deployment() {
  start_group "Deployment Verification"
  
  # Ensure DEPLOY_DIR is valid
  if [ -z "$DEPLOY_DIR" ] || [ ! -d "$DEPLOY_DIR" ]; then
    log_error "Deployment directory $DEPLOY_DIR not found"
    return 1
  fi

  cd "$DEPLOY_DIR" || return
  
  log_info "Checking container health..."
  
  local healthy
  healthy=true
  
  local container_ids
  container_ids=$(sudo docker compose ps -q)
  
  if [ -z "$container_ids" ]; then
    log_error "No containers found for this project."
    end_group
    return 1
  fi

  for id in $container_ids; do
    # Get Name, Status, and Health in one go
    local info
    info=$(sudo docker inspect --format='{{.Name}}|{{.State.Status}}|{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' "$id")
    
    local name
    name=$(echo "$info" | cut -d'|' -f1 | sed 's/\///')
    local status
    status=$(echo "$info" | cut -d'|' -f2)
    local health
    health=$(echo "$info" | cut -d'|' -f3)
    
    if [ "$status" = "running" ]; then
      if [ "$health" = "unhealthy" ]; then
        log_error "Container $name is RUNNING but UNHEALTHY"
        healthy=false
        echo "::group::Logs for container $name"
        sudo docker logs "$id" 2>&1 | tee -a "$LOG_FILE"
        echo "::endgroup::"
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

verify_deployment