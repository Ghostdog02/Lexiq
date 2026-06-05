#!/bin/sh

ENVIRONMENT=${ENVIRONMENT:-development}

echo "Starting Nginx in $ENVIRONMENT mode..."

if [ "$ENVIRONMENT" = "production" ]; then
    echo "Using production configuration..."
    cp /etc/nginx/nginx.prod.conf /etc/nginx/nginx.conf
else
    echo "Using development configuration..."
    cp /etc/nginx/nginx.dev.conf /etc/nginx/nginx.conf
fi

exec "$@"
