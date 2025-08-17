#!/bin/bash

# Script to enable nginx on standard ports 80/443 (requires sudo)

set -e

DOMAIN="sparkify.dev"

echo "🔒 Enabling nginx on standard ports 80/443..."
echo "This requires administrator privileges."
echo ""

# Check if LaunchDaemon already exists
if [ -f "/Library/LaunchDaemons/dev.sparkify.nginx.plist" ]; then
    echo "🔄 nginx LaunchDaemon already exists. Updating..."
    sudo launchctl unload /Library/LaunchDaemons/dev.sparkify.nginx.plist 2>/dev/null || true
fi

# Stop user-level nginx service
echo "Stopping user-level nginx service..."
brew services stop nginx 2>/dev/null || true

# Create nginx config for standard ports
echo "Creating nginx configuration for ports 80/443..."

# Determine nginx configuration directory
if [ -d "/opt/homebrew/etc/nginx" ]; then
    NGINX_DIR="/opt/homebrew/etc/nginx"
elif [ -d "/usr/local/etc/nginx" ]; then
    NGINX_DIR="/usr/local/etc/nginx"
else
    echo "❌ Could not find nginx configuration directory"
    exit 1
fi

# Use the nginx.conf file (already configured for standard ports)
sed -e "s|/Users/antbly/dev/procurement|$(pwd)|g" \
    -e "s|HOMEBREW_PREFIX|$(brew --prefix)|g" \
    ./nginx.conf > "$NGINX_DIR/nginx.conf"

# Test nginx configuration
if ! nginx -t; then
    echo "❌ nginx configuration is invalid"
    exit 1
fi

# Create LaunchDaemon plist
echo "Creating system LaunchDaemon..."

cat > ~/nginx.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>dev.sparkify.nginx</string>
    <key>ProgramArguments</key>
    <array>
        <string>$(brew --prefix)/bin/nginx</string>
        <string>-g</string>
        <string>daemon off;</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>$(brew --prefix)</string>
    <key>StandardOutPath</key>
    <string>$(brew --prefix)/var/log/nginx/access.log</string>
    <key>StandardErrorPath</key>
    <string>$(brew --prefix)/var/log/nginx/error.log</string>
</dict>
</plist>
EOF

# Install and start LaunchDaemon
echo "Installing LaunchDaemon (requires sudo)..."
cp ~/nginx.plist /Library/LaunchDaemons/dev.sparkify.nginx.plist
chown root:wheel /Library/LaunchDaemons/dev.sparkify.nginx.plist
launchctl load /Library/LaunchDaemons/dev.sparkify.nginx.plist

# Wait a moment for nginx to start
sleep 2

# Test the connection
echo ""
echo "🔍 Testing nginx on standard ports..."

if curl -s -o /dev/null -w "%{http_code}" http://localhost | grep -q "200\|301\|302"; then
    echo "✅ HTTP (port 80) is working"
else
    echo "❌ HTTP (port 80) is not responding"
fi

if curl -s -o /dev/null -w "%{http_code}" -k https://localhost | grep -q "200\|301\|302"; then
    echo "✅ HTTPS (port 443) is working"
else
    echo "❌ HTTPS (port 443) is not responding"
fi

echo ""
echo "🎉 nginx is now running on standard ports!"
echo ""
echo "🌐 Access your application:"
echo "   - HTTPS: https://$DOMAIN"
echo "   - HTTP:  http://$DOMAIN (redirects to HTTPS)"
echo ""
echo "📝 To stop nginx:"
echo "   sudo launchctl unload /Library/LaunchDaemons/dev.sparkify.nginx.plist"