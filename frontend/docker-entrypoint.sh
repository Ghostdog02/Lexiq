#!/bin/sh

ENVIRONMENT=${ENVIRONMENT:-development}

echo "Starting Nginx in $ENVIRONMENT mode..."

if [ "$ENVIRONMENT" = "production" ]; then
    # Check both domain certificate files - both must exist to start in HTTPS mode.
    # On a fresh deployment the letsencrypt-certs volume is empty, so we fall back
    # to HTTP-only mode so that ACME challenges can be served. Once init-letsencrypt.sh
    # issues real certificates it will switch nginx to HTTPS via `nginx -s reload`.
    if [ -r /etc/letsencrypt/live/lexiqlanguage.eu/fullchain.pem ] && \
       [ -r /etc/letsencrypt/live/api.lexiqlanguage.eu/fullchain.pem ]; then
        echo "SSL certificates found, using production HTTPS configuration..."
        cp /etc/nginx/nginx.prod.conf /etc/nginx/nginx.conf
    else
        echo "SSL certificates not found. Starting in ACME challenge mode (HTTP only)."
        echo "Run scripts/init-letsencrypt.sh on the server to provision certificates."
        cp /etc/nginx/nginx.acme-only.conf /etc/nginx/nginx.conf
    fi
else
    echo "Using development configuration..."
    cp /etc/nginx/nginx.dev.conf /etc/nginx/nginx.conf
fi

exec "$@"
