#!/bin/bash

# Let's Encrypt certificate initialization script
# Run this once to generate initial certificates before starting the full stack

set -e

DOMAINS=(lexiqlanguage.eu www.lexiqlanguage.eu api.lexiqlanguage.eu)
EMAIL="admin@lexiqlanguage.eu"
STAGING=0  # Set to 1 for testing (avoids rate limits)

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=== Let's Encrypt Certificate Initialization ===${NC}"

# Check if running as root or with sudo
if [ "$EUID" -ne 0 ]; then
    echo -e "${RED}Please run with sudo${NC}"
    exit 1
fi

# Create required directories
echo -e "${YELLOW}Creating directories...${NC}"
mkdir -p /etc/letsencrypt
mkdir -p ./certbot/www

# Download recommended TLS parameters if not present
if [ ! -f "/etc/letsencrypt/options-ssl-nginx.conf" ]; then
    echo -e "${YELLOW}Downloading recommended TLS parameters...${NC}"
    curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot-nginx/certbot_nginx/_internal/tls_configs/options-ssl-nginx.conf > /etc/letsencrypt/options-ssl-nginx.conf
fi

if [ ! -f "/etc/letsencrypt/ssl-dhparams.pem" ]; then
    echo -e "${YELLOW}Downloading DH parameters...${NC}"
    curl -s https://raw.githubusercontent.com/certbot/certbot/master/certbot/certbot/ssl-dhparams.pem > /etc/letsencrypt/ssl-dhparams.pem
fi

# Create dummy certificates for nginx to start
echo -e "${YELLOW}Creating dummy certificates for initial nginx startup...${NC}"
for domain in "${DOMAINS[@]}"; do
    # Skip www subdomain (uses same cert as main domain)
    if [[ "$domain" == www.* ]]; then
        continue
    fi

    cert_path="/etc/letsencrypt/live/$domain"
    if [ ! -d "$cert_path" ]; then
        mkdir -p "$cert_path"
        openssl req -x509 -nodes -newkey rsa:2048 -days 1 \
            -keyout "$cert_path/privkey.pem" \
            -out "$cert_path/fullchain.pem" \
            -subj "/CN=localhost"
        echo -e "${GREEN}Created dummy cert for $domain${NC}"
    fi
done

# Start nginx temporarily to handle ACME challenges
echo -e "${YELLOW}Starting nginx for ACME challenge...${NC}"
docker compose -f docker-compose.prod.yml up -d frontend

# Wait for nginx to start
sleep 5

# Delete dummy certificates
echo -e "${YELLOW}Removing dummy certificates...${NC}"
for domain in "${DOMAINS[@]}"; do
    if [[ "$domain" == www.* ]]; then
        continue
    fi
    rm -rf "/etc/letsencrypt/live/$domain"
    rm -rf "/etc/letsencrypt/archive/$domain"
    rm -rf "/etc/letsencrypt/renewal/$domain.conf"
done

# Request real certificates
echo -e "${YELLOW}Requesting Let's Encrypt certificates...${NC}"

staging_arg=""
if [ $STAGING -eq 1 ]; then
    staging_arg="--staging"
    echo -e "${YELLOW}Using staging environment (for testing)${NC}"
fi

# Build domain arguments
domain_args=""
for domain in "${DOMAINS[@]}"; do
    domain_args="$domain_args -d $domain"
done

docker run --rm \
    -v /etc/letsencrypt:/etc/letsencrypt \
    -v ./certbot/www:/var/www/certbot \
    certbot/certbot certonly \
    --webroot \
    -w /var/www/certbot \
    $staging_arg \
    --email $EMAIL \
    --agree-tos \
    --no-eff-email \
    --force-renewal \
    $domain_args

# Reload nginx to use real certificates
echo -e "${YELLOW}Reloading nginx with real certificates...${NC}"
docker compose -f docker-compose.prod.yml exec frontend nginx -s reload

echo -e "${GREEN}=== Certificate initialization complete! ===${NC}"
echo -e "${GREEN}You can now start the full stack with: docker compose -f docker-compose.prod.yml up -d${NC}"
