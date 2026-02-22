#!/bin/sh

# Certbot sidecar entrypoint.
# Fixes certificate permissions for nginx-unprivileged on startup, then exits.
# Certificate renewal is handled by the weekly infrastructure-update workflow.

set -e

# ── Fix permissions on startup ────────────────────────────────────────
# Certbot creates archive/ with 0700 and privkeys with 0600.
# nginx-unprivileged can't traverse live/ → archive/ symlinks.
# Make directories traversable and cert files readable by all users.
fix_permissions() {
  chmod 755 /etc/letsencrypt/live 2>/dev/null
  find /etc/letsencrypt/archive -type d -exec chmod 755 {} + 2>/dev/null
  find /etc/letsencrypt/archive -type f -exec chmod 644 {} + 2>/dev/null
}

if [ -d /etc/letsencrypt/archive ]; then
  echo "Fixing certificate permissions..."
  fix_permissions
  echo "Certificate permissions fixed."
fi
