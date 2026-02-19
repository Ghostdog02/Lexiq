#!/bin/sh

# Certbot sidecar entrypoint.
# 1. Fixes certificate permissions for nginx-unprivileged on startup.
# 2. Runs a renewal loop every 12 hours with a deploy-hook that
#    re-applies permissions after each successful renewal.

set -e

trap exit TERM

# ── Fix permissions on startup ────────────────────────────────────────
# Certbot creates archive/ with 0700 and privkeys with 0600.
# nginx-unprivileged can't traverse live/ → archive/ symlinks.
# Make directories traversable and cert files readable by all users.
fix_permissions() {
  find /etc/letsencrypt/archive -type d -exec chmod 755 {} + 2>/dev/null
  find /etc/letsencrypt/archive -type f -exec chmod 644 {} + 2>/dev/null
}

if [ -d /etc/letsencrypt/archive ]; then
  echo "Fixing certificate permissions..."
  fix_permissions
  echo "Certificate permissions fixed."
fi

# ── Renewal loop ──────────────────────────────────────────────────────
# certbot renew checks all managed certs; --deploy-hook runs only when
# a cert is actually renewed (re-applies permissions for the new files).
echo "Starting certbot renewal loop (every 12h)..."
while :; do
  certbot renew \
    --webroot -w /var/www/certbot \
    --quiet \
    --deploy-hook "find /etc/letsencrypt/archive -type d -exec chmod 755 {} + && find /etc/letsencrypt/archive -type f -exec chmod 644 {} +"
  sleep 12h &
  wait $!
done
