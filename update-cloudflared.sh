#!/usr/bin/env bash
set -euo pipefail

# Update Cloudflare Tunnel config to use plain HTTP origin on localhost:80
# and include sparkify.dev hostname.

UUID="392abfe9-a2db-41dd-a688-69e887823cdc"
CONF_ETC="/etc/cloudflared/config.yml"
CONF_USER="$HOME/.cloudflared/config.yml"
CRED_ETC="/etc/cloudflared/${UUID}.json"

echo "🔧 Updating cloudflared configuration..."

if [ ! -f "$CRED_ETC" ]; then
  echo "⚠️  Credentials not found at $CRED_ETC"
  echo "    If credentials live in ~/.cloudflared, copy them:"
  echo "    sudo cp $HOME/.cloudflared/${UUID}.json $CRED_ETC && sudo chmod 600 $CRED_ETC"
fi

tmp_etc=$(mktemp)
cat > "$tmp_etc" <<YAML
tunnel: ${UUID}
credentials-file: ${CRED_ETC}

ingress:
  - hostname: sparkify.dev
    service: http://localhost:80
  - service: http_status:404
YAML

tmp_user=$(mktemp)
cat > "$tmp_user" <<YAML
tunnel: ${UUID}
credentials-file: $HOME/.cloudflared/${UUID}.json

ingress:
  - hostname: sparkify.dev
    service: http://localhost:80
  - service: http_status:404
YAML

echo "📦 Backing up existing configs (if present)..."
if [ -f "$CONF_ETC" ]; then
  sudo cp "$CONF_ETC" "${CONF_ETC}.bak-$(date +%Y%m%d-%H%M%S)"
fi
if [ -f "$CONF_USER" ]; then
  cp "$CONF_USER" "${CONF_USER}.bak-$(date +%Y%m%d-%H%M%S)"
fi

echo "✍️  Writing /etc/cloudflared/config.yml..."
sudo install -d -m 755 /etc/cloudflared
sudo install -m 644 "$tmp_etc" "$CONF_ETC"

echo "✍️  Writing $CONF_USER ..."
mkdir -p "$(dirname "$CONF_USER")"
install -m 644 "$tmp_user" "$CONF_USER"

echo "🧪 Validating new config syntax..."
if command -v cloudflared >/dev/null 2>&1; then
  cloudflared tunnel ingress validate -f "$CONF_ETC" || {
    echo "❌ Validation failed. Restoring backup."; 
    [ -f "${CONF_ETC}.bak" ] && sudo cp "${CONF_ETC}.bak" "$CONF_ETC"; 
    exit 1; 
  }
else
  echo "⚠️  cloudflared not found in PATH. Skipping validation."
fi

echo "🔄 Restarting cloudflared LaunchDaemon..."
if launchctl list | grep -q com.cloudflare.sparkify; then
  sudo launchctl stop com.cloudflare.sparkify || true
  sleep 1
  sudo launchctl start com.cloudflare.sparkify || true
  echo "✅ cloudflared restarted"
else
  echo "⚠️  LaunchDaemon not loaded. Start with:"
  echo "    sudo launchctl load /Library/LaunchDaemons/com.cloudflare.sparkify.plist"
fi

echo "🎉 Cloudflare configuration updated."
echo "   - /etc/cloudflared/config.yml -> http://localhost:80"
echo "   - Hostname: sparkify.dev"
echo "   - Credentials: $CRED_ETC"

