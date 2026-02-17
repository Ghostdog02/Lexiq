#!/bin/bash

# Let's Encrypt certificate initialization script.
# Run this ONCE on a fresh server before the full stack is running.
#
# What this script does:
#   1. Starts nginx in HTTP-only ACME mode (no certs required)
#   2. Writes options-ssl-nginx.conf and ssl-dhparams.pem into the Docker volume
#      (certbot's webroot renewal never creates these; only certbot --nginx does)
#   3. Issues real certificates via certbot --webroot
#   4. Switches nginx to HTTPS and reloads
#
# Usage:
#   cd /path/to/lexiq
#   scripts/init-letsencrypt.sh
#
# Set STAGING=1 to use Let's Encrypt staging environment (avoids rate limits during testing).

set -euo pipefail

DOMAINS=(lexiqlanguage.eu www.lexiqlanguage.eu api.lexiqlanguage.eu)
EMAIL="admin@lexiqlanguage.eu"
STAGING=${STAGING:-0}
COMPOSE_FILE="docker-compose.prod.yml"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

info()    { echo -e "${YELLOW}[init] $*${NC}"; }
success() { echo -e "${GREEN}[init] $*${NC}"; }
error()   { echo -e "${RED}[init] ERROR: $*${NC}" >&2; }

echo -e "${GREEN}=== Let's Encrypt Certificate Initialization ===${NC}"

# ── Step 1: Start nginx in HTTP-only ACME mode ─────────────────────────────
# The entrypoint detects no certs → copies nginx.acme-only.conf automatically.
info "Starting nginx in HTTP-only ACME mode..."
docker compose -f "$COMPOSE_FILE" up -d frontend

info "Waiting for nginx to start..."
sleep 5

if ! docker compose -f "$COMPOSE_FILE" exec frontend nginx -t -q 2>/dev/null; then
    error "nginx failed to start. Check logs with: docker compose logs frontend"
    exit 1
fi
success "nginx is running in HTTP-only mode"

# ── Step 2: Write static SSL config files into the Docker volume ────────────
# certbot's --webroot mode only issues/renews cert files. It never writes
# options-ssl-nginx.conf or ssl-dhparams.pem (that's the --nginx plugin's job).
# We download them from certbot's GitHub into the letsencrypt-certs volume
# by running a one-shot command inside the certbot service container.
info "Writing options-ssl-nginx.conf and ssl-dhparams.pem into Docker volume..."
docker compose -f "$COMPOSE_FILE" run --rm \
    --entrypoint sh \
    certbot \
    -c "
        wget -q -O /etc/letsencrypt/options-ssl-nginx.conf \
            'https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf' && \
        wget -q -O /etc/letsencrypt/ssl-dhparams.pem \
            'https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem' && \
        echo 'SSL config files written successfully'
    "
success "SSL config files written to volume"

# ── Step 3: Issue real certificates ─────────────────────────────────────────
info "Requesting Let's Encrypt certificates..."

staging_arg=""
if [ "$STAGING" -eq 1 ]; then
    staging_arg="--staging"
    info "Using staging environment (avoids rate limits)"
fi

domain_args=""
for domain in "${DOMAINS[@]}"; do
    domain_args="$domain_args -d $domain"
done

# Override the sidecar's renewal entrypoint with a one-shot certonly command.
# The certbot service mounts letsencrypt-certs:/etc/letsencrypt and
# certbot-webroot:/var/www/certbot — the same volumes nginx uses.
docker compose -f "$COMPOSE_FILE" run --rm \
    --entrypoint certbot \
    certbot certonly \
    --webroot \
    -w /var/www/certbot \
    $staging_arg \
    --email "$EMAIL" \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    $domain_args

success "Certificates issued successfully"

# ── Step 4: Switch nginx to HTTPS ───────────────────────────────────────────
# Copy the production HTTPS config into the running container and reload.
# nginx will now find all the cert files that were just written to the volume.
info "Switching nginx to HTTPS configuration..."
docker compose -f "$COMPOSE_FILE" exec frontend sh -c \
    "cp /etc/nginx/nginx.prod.conf /etc/nginx/nginx.conf && nginx -s reload"

success "nginx is now running in HTTPS mode"
echo -e "${GREEN}=== Certificate initialization complete! ===${NC}"
echo -e "${GREEN}The full stack can now be started: docker compose -f $COMPOSE_FILE up -d${NC}"
