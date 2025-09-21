#!/usr/bin/env bash
set -euo pipefail

# Update Cloudflare Tunnel config to use plain HTTP origin on localhost:80
# and include sparkify.dev and sparkify.com hostnames.

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
  - hostname: sparkify.com
    service: http://localhost:80
  - hostname: sparkify.dev
    service: http://localhost:80
  - service: http_status:404
YAML

tmp_user=$(mktemp)
cat > "$tmp_user" <<YAML
tunnel: ${UUID}
credentials-file: $HOME/.cloudflared/${UUID}.json

ingress:
  - hostname: sparkify.com
    service: http://localhost:80
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
  if ! cloudflared --config "$CONF_ETC" tunnel ingress validate; then
    echo "❌ Validation failed. Restoring backup."
    latest_bak=$(ls -1t ${CONF_ETC}.bak-* 2>/dev/null | head -n1 || true)
    if [ -n "$latest_bak" ]; then
      sudo cp "$latest_bak" "$CONF_ETC"
      echo "↩️  Restored $latest_bak"
    fi
    exit 1
  fi
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
  if [ -f "/Library/LaunchDaemons/com.cloudflare.sparkify.plist" ]; then
    echo "ℹ️  LaunchDaemon not loaded. Loading now..."
    sudo launchctl load /Library/LaunchDaemons/com.cloudflare.sparkify.plist || true
    sleep 1
    sudo launchctl start com.cloudflare.sparkify || true
  else
    echo "⚠️  LaunchDaemon plist missing at /Library/LaunchDaemons/com.cloudflare.sparkify.plist"
    echo "   Run system-setup.sh to create it, or start manually:"
    echo "   cloudflared --config $CONF_ETC tunnel run sparkify"
  fi
fi

echo "🎉 Cloudflare configuration updated."
echo "   - /etc/cloudflared/config.yml -> http://localhost:80"
echo "   - Hostnames: sparkify.com, sparkify.dev"
echo "   - Credentials: $CRED_ETC"
