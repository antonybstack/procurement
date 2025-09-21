#!/usr/bin/env bash
set -euo pipefail

# Sync the repo nginx.conf into Homebrew's nginx directory and restart the service.
# This aligns the active nginx with the HTTP-only config used with Cloudflare Tunnel.

PROJECT_DIR=$(cd "$(dirname "$0")" && pwd)

if ! command -v brew >/dev/null 2>&1; then
  echo "❌ Homebrew not found. Install Homebrew first."
  exit 1
fi

HOMEBREW_PREFIX=$(brew --prefix)
NGINX_DIR="$HOMEBREW_PREFIX/etc/nginx"
NGINX_BIN="$HOMEBREW_PREFIX/bin/nginx"

if [ ! -d "$NGINX_DIR" ]; then
  echo "❌ nginx config dir not found at $NGINX_DIR"
  echo "   Install nginx: brew install nginx"
  exit 1
fi

if [ ! -f "$PROJECT_DIR/nginx.conf" ]; then
  echo "❌ Repo nginx.conf not found at $PROJECT_DIR/nginx.conf"
  exit 1
fi

echo "📦 Backing up current nginx.conf..."
if [ -f "$NGINX_DIR/nginx.conf" ]; then
  sudo cp "$NGINX_DIR/nginx.conf" "$NGINX_DIR/nginx.conf.bak-$(date +%Y%m%d-%H%M%S)"
fi

echo "✍️  Writing HTTP-only nginx.conf to $NGINX_DIR ..."
tmp_conf=$(mktemp)
sed -e "s|HOMEBREW_PREFIX|$HOMEBREW_PREFIX|g" \
    -e "s|/Users/antbly/dev/procurement|$PROJECT_DIR|g" \
    "$PROJECT_DIR/nginx.conf" > "$tmp_conf"

sudo install -m 644 "$tmp_conf" "$NGINX_DIR/nginx.conf"
rm -f "$tmp_conf"

echo "📝 Ensuring log dir exists..."
sudo install -d -m 755 "$HOMEBREW_PREFIX/var/log/nginx"

echo "🧪 Validating nginx configuration..."
if sudo "$NGINX_BIN" -t; then
  echo "✅ nginx config test passed"
else
  echo "❌ nginx config test failed"
  exit 1
fi

echo "🔄 Restarting nginx LaunchDaemon..."
sudo launchctl stop dev.sparkify.nginx || true
sleep 1
sudo launchctl start dev.sparkify.nginx || true

echo "🎉 nginx updated and restarted."
echo "   Active config: $NGINX_DIR/nginx.conf"
echo "   Logs: $HOMEBREW_PREFIX/var/log/nginx/error.log"

