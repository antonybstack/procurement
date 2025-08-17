#!/bin/bash

# System-level setup script for Procurement Management System
# This script configures system infrastructure and must be run with sudo
# Usage: sudo ./system-setup.sh

set -e

if [ "$EUID" -ne 0 ]; then
    echo "❌ This script must be run with sudo"
    echo "Usage: sudo ./system-setup.sh"
    exit 1
fi

echo "🔧 System Infrastructure Setup"
echo "============================="
echo ""

DOMAIN="sparkify.dev"
ORIGINAL_USER="${SUDO_USER:-$(logname)}"
ORIGINAL_HOME=$(eval echo "~$ORIGINAL_USER")

echo "🔍 Running as: root"
echo "🔍 Original user: $ORIGINAL_USER"
echo "🔍 Original home: $ORIGINAL_HOME"
echo ""

# Step 1: Configure /etc/hosts
echo "🌐 Step 1: Configuring system hosts file..."
HOSTS_ENTRY="127.0.0.1 $DOMAIN"

if ! grep -q "$DOMAIN" /etc/hosts; then
    echo "Adding $DOMAIN to /etc/hosts..."
    echo "$HOSTS_ENTRY" >> /etc/hosts
    echo "✅ Added $DOMAIN to /etc/hosts"
else
    echo "✅ $DOMAIN already exists in /etc/hosts"
fi

# Step 2: Set up SSL certificates in system directory
echo ""
echo "🔒 Step 2: Setting up system SSL certificates..."

SSL_SYSTEM_DIR="/etc/ssl/sparkify"
SSL_USER_DIR="$ORIGINAL_HOME/dev/procurement/ssl"

mkdir -p "$SSL_SYSTEM_DIR"

if [ -f "$SSL_USER_DIR/$DOMAIN.pem" ]; then
    echo "Copying SSL certificates from user directory..."
    cp "$SSL_USER_DIR/$DOMAIN.pem" "$SSL_SYSTEM_DIR/"
    cp "$SSL_USER_DIR/$DOMAIN-key.pem" "$SSL_SYSTEM_DIR/"
    
    # Set proper permissions for nginx to read certificates
    chmod 644 "$SSL_SYSTEM_DIR/$DOMAIN.pem"
    chmod 640 "$SSL_SYSTEM_DIR/$DOMAIN-key.pem"
    chown root:admin "$SSL_SYSTEM_DIR/$DOMAIN-key.pem"
    
    echo "✅ SSL certificates installed in $SSL_SYSTEM_DIR with proper permissions"
else
    echo "⚠️  SSL certificates not found in $SSL_USER_DIR"
    echo "    Run mkcert as user first, then re-run this script"
fi

# Step 3: Set up cloudflared in system directory
echo ""
echo "☁️ Step 3: Setting up system cloudflared configuration..."

CLOUDFLARED_SYSTEM_DIR="/etc/cloudflared"
CLOUDFLARED_USER_DIR="$ORIGINAL_HOME/.cloudflared"

mkdir -p "$CLOUDFLARED_SYSTEM_DIR"

if [ -d "$CLOUDFLARED_USER_DIR" ]; then
    echo "Copying cloudflared config from user directory..."
    cp "$CLOUDFLARED_USER_DIR/config.yml" "$CLOUDFLARED_SYSTEM_DIR/" 2>/dev/null || echo "⚠️  config.yml not found"
    cp "$CLOUDFLARED_USER_DIR/cert.pem" "$CLOUDFLARED_SYSTEM_DIR/" 2>/dev/null || echo "⚠️  cert.pem not found"
    cp "$CLOUDFLARED_USER_DIR"/*.json "$CLOUDFLARED_SYSTEM_DIR/" 2>/dev/null || echo "⚠️  credentials file not found"
    
    # Set proper permissions
    chmod 644 "$CLOUDFLARED_SYSTEM_DIR/config.yml" 2>/dev/null || true
    chmod 644 "$CLOUDFLARED_SYSTEM_DIR/cert.pem" 2>/dev/null || true
    chmod 600 "$CLOUDFLARED_SYSTEM_DIR"/*.json 2>/dev/null || true
    
    echo "✅ Cloudflared config installed in $CLOUDFLARED_SYSTEM_DIR"
else
    echo "⚠️  Cloudflared config not found in $CLOUDFLARED_USER_DIR"
    echo "    Set up cloudflared as user first, then re-run this script"
fi

# Step 4: Configure nginx configuration and LaunchDaemon
echo ""
echo "🌐 Step 4: Setting up nginx configuration and system service..."

# Determine nginx path
if command -v /opt/homebrew/bin/nginx &> /dev/null; then
    NGINX_PATH="/opt/homebrew/bin/nginx"
    HOMEBREW_PREFIX="/opt/homebrew"
elif command -v /usr/local/bin/nginx &> /dev/null; then
    NGINX_PATH="/usr/local/bin/nginx"
    HOMEBREW_PREFIX="/usr/local"
else
    echo "❌ nginx not found. Install with: brew install nginx"
    exit 1
fi

# Configure nginx
echo "Configuring nginx with system SSL certificates..."

# Determine nginx configuration directory
if [ -d "$HOMEBREW_PREFIX/etc/nginx" ]; then
    NGINX_DIR="$HOMEBREW_PREFIX/etc/nginx"
else
    echo "❌ Could not find nginx configuration directory"
    exit 1
fi

# Create log directory
mkdir -p "$HOMEBREW_PREFIX/var/log/nginx"

# Update nginx config to use system SSL certificates
PROJECT_DIR="$ORIGINAL_HOME/dev/procurement"
if [ -f "$PROJECT_DIR/nginx.conf" ]; then
    echo "Copying and configuring nginx configuration..."
    
    # Process nginx.conf template with proper paths
    sed -e "s|HOMEBREW_PREFIX|$HOMEBREW_PREFIX|g" \
        -e "s|/Users/antbly/dev/procurement|$PROJECT_DIR|g" \
        "$PROJECT_DIR/nginx.conf" > "$NGINX_DIR/nginx.conf"
    
    # Verify nginx configuration can access system SSL certificates
    if nginx -t; then
        echo "✅ nginx configuration is valid"
    else
        echo "❌ nginx configuration is invalid - check SSL certificate permissions"
        exit 1
    fi
else
    echo "⚠️  nginx.conf not found in $PROJECT_DIR"
fi

# Create nginx LaunchDaemon
cat > /Library/LaunchDaemons/dev.sparkify.nginx.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>dev.sparkify.nginx</string>
    <key>ProgramArguments</key>
    <array>
        <string>$NGINX_PATH</string>
        <string>-g</string>
        <string>daemon off;</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>$HOMEBREW_PREFIX</string>
    <key>StandardOutPath</key>
    <string>$HOMEBREW_PREFIX/var/log/nginx/access.log</string>
    <key>StandardErrorPath</key>
    <string>$HOMEBREW_PREFIX/var/log/nginx/error.log</string>
</dict>
</plist>
EOF

chown root:wheel /Library/LaunchDaemons/dev.sparkify.nginx.plist
chmod 644 /Library/LaunchDaemons/dev.sparkify.nginx.plist

# Stop any existing nginx processes
pkill nginx 2>/dev/null || true
launchctl unload /Library/LaunchDaemons/dev.sparkify.nginx.plist 2>/dev/null || true

echo "✅ nginx LaunchDaemon created"

# Step 5: Configure cloudflared LaunchDaemon
echo ""
echo "☁️ Step 5: Setting up cloudflared system service..."

# Find cloudflared path
if command -v /opt/homebrew/bin/cloudflared &> /dev/null; then
    CLOUDFLARED_PATH="/opt/homebrew/bin/cloudflared"
elif command -v /usr/local/bin/cloudflared &> /dev/null; then
    CLOUDFLARED_PATH="/usr/local/bin/cloudflared"
else
    echo "❌ cloudflared not found. Install with: brew install cloudflared"
    exit 1
fi

# Create cloudflared LaunchDaemon
cat > /Library/LaunchDaemons/com.cloudflare.sparkify.plist << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.cloudflare.sparkify</string>
    <key>ProgramArguments</key>
    <array>
        <string>$CLOUDFLARED_PATH</string>
        <string>tunnel</string>
        <string>--loglevel</string>
        <string>info</string>
        <string>--transport-loglevel</string>
        <string>warn</string>
        <string>--retries</string>
        <string>5</string>
        <string>--protocol</string>
        <string>quic</string>
        <string>run</string>
        <string>sparkify</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <dict>
        <key>SuccessfulExit</key>
        <false/>
        <key>NetworkState</key>
        <true/>
    </dict>
    <key>ThrottleInterval</key>
    <integer>10</integer>
    <key>StandardOutPath</key>
    <string>/var/log/cloudflared.log</string>
    <key>StandardErrorPath</key>
    <string>/var/log/cloudflared.log</string>
    <key>WorkingDirectory</key>
    <string>/etc/cloudflared</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>TUNNEL_TRANSPORT_PROTOCOL</key>
        <string>quic</string>
        <key>TUNNEL_ORIGIN_CERT</key>
        <string>/etc/cloudflared/cert.pem</string>
    </dict>
</dict>
</plist>
EOF

chown root:wheel /Library/LaunchDaemons/com.cloudflare.sparkify.plist
chmod 644 /Library/LaunchDaemons/com.cloudflare.sparkify.plist

# Stop any existing cloudflared processes
pkill cloudflared 2>/dev/null || true
launchctl unload /Library/LaunchDaemons/com.cloudflare.sparkify.plist 2>/dev/null || true

echo "✅ cloudflared LaunchDaemon created"

# Step 6: Configure power management
echo ""
echo "⚡ Step 6: Configuring power management..."

# Check current settings and only change what's needed
echo "Checking current power management settings..."

# Check and set networkoversleep (should be 1)
current_networkoversleep=$(pmset -g | grep networkoversleep | awk '{print $2}')
if [ "$current_networkoversleep" != "1" ]; then
    pmset -a networkoversleep 1 && echo "✅ Network over sleep enabled" || echo "⚠️  Failed to enable network over sleep"
else
    echo "✅ Network over sleep already enabled"
fi

# Check and set tcpkeepalive (should be 1)
current_tcpkeepalive=$(pmset -g | grep tcpkeepalive | awk '{print $2}')
if [ "$current_tcpkeepalive" != "1" ]; then
    pmset -a tcpkeepalive 1 && echo "✅ TCP keep-alive enabled" || echo "⚠️  Failed to enable TCP keep-alive"
else
    echo "✅ TCP keep-alive already enabled"
fi

# Check and set powernap (should be 0)
current_powernap=$(pmset -g | grep powernap | awk '{print $2}')
if [ "$current_powernap" != "0" ]; then
    pmset -a powernap 0 && echo "✅ Power nap disabled" || echo "⚠️  Failed to disable power nap"
else
    echo "✅ Power nap already disabled"
fi

# Check and set standby (should be 0)
current_standby=$(pmset -g | grep standby | awk '{print $2}')
if [ "$current_standby" != "0" ]; then
    pmset -a standby 0 && echo "✅ Standby disabled" || echo "⚠️  Failed to disable standby"
else
    echo "✅ Standby already disabled"
fi

# Check and set displaysleep (should be 0)
current_displaysleep=$(pmset -g | grep displaysleep | awk '{print $2}')
if [ "$current_displaysleep" != "0" ]; then
    pmset -a displaysleep 0 && echo "✅ Display sleep disabled" || echo "⚠️  Failed to disable display sleep"
else
    echo "✅ Display sleep already disabled"
fi

# Additional production server optimizations
echo ""
echo "🔧 Applying additional production server optimizations..."

# Ensure SleepServices is disabled (already 0 in your setup)
current_sleepservices=$(pmset -g | grep SleepServices | awk '{print $2}')
if [ "$current_sleepservices" != "0" ]; then
    pmset -a SleepServices 0 && echo "✅ Sleep services disabled" || echo "⚠️  Failed to disable sleep services"
else
    echo "✅ Sleep services already disabled"
fi

# Ensure disksleep is disabled (already 0 in your setup)
current_disksleep=$(pmset -g | grep disksleep | awk '{print $2}')
if [ "$current_disksleep" != "0" ]; then
    pmset -a disksleep 0 && echo "✅ Disk sleep disabled" || echo "⚠️  Failed to disable disk sleep"
else
    echo "✅ Disk sleep already disabled"
fi

# Ensure womp (Wake on Magic Packet) is enabled (already 1 in your setup)
current_womp=$(pmset -g | grep womp | awk '{print $2}')
if [ "$current_womp" != "1" ]; then
    pmset -a womp 1 && echo "✅ Wake on Magic Packet enabled" || echo "⚠️  Failed to enable Wake on Magic Packet"
else
    echo "✅ Wake on Magic Packet already enabled"
fi

# Ensure autorestart is enabled (already 1 in your setup)
current_autorestart=$(pmset -g | grep autorestart | awk '{print $2}')
if [ "$current_autorestart" != "1" ]; then
    pmset -a autorestart 1 && echo "✅ Auto-restart after power failure enabled" || echo "⚠️  Failed to enable auto-restart"
else
    echo "✅ Auto-restart after power failure already enabled"
fi

# Step 7: Additional macOS production server optimizations
echo ""
echo "🔧 Step 7: Additional production server optimizations..."

# Configure system to prioritize network and server performance
echo "Configuring system performance optimizations..."

# Increase file descriptor limits for server applications
echo "kern.maxfiles=65536" >> /etc/sysctl.conf 2>/dev/null || echo "# File descriptor limits already configured"
echo "kern.maxfilesperproc=32768" >> /etc/sysctl.conf 2>/dev/null || echo "# Per-process file descriptor limits already configured"

# Optimize network buffer sizes
echo "net.inet.tcp.sendspace=65536" >> /etc/sysctl.conf 2>/dev/null || echo "# TCP send buffer already configured"
echo "net.inet.tcp.recvspace=65536" >> /etc/sysctl.conf 2>/dev/null || echo "# TCP receive buffer already configured"

# Disable automatic software updates for production stability
echo "🔄 Configuring automatic updates..."
softwareupdate --schedule off 2>/dev/null || echo "⚠️  Could not disable automatic updates (may require manual configuration)"

# Configure remote management (if needed)
echo "🖥️  Remote management recommendations:"
echo "   Enable SSH: sudo systemsetup -setremotelogin on"
echo "   Enable VNC: sudo /System/Library/CoreServices/RemoteManagement/ARDAgent.app/Contents/Resources/kickstart -activate -configure -access -on -restart -agent -privs -all"

echo ""
echo "🎉 System infrastructure setup complete!"
echo "======================================"
echo ""
echo "📋 What was configured:"
echo "   ✅ System hosts file (/etc/hosts)"
echo "   ✅ SSL certificates (/etc/ssl/sparkify/)"
echo "   ✅ Cloudflared config (/etc/cloudflared/)"
echo "   ✅ nginx LaunchDaemon (system service)"
echo "   ✅ cloudflared LaunchDaemon (system service)"
echo "   ✅ Power management settings"
echo ""
echo "🔄 Next steps:"
echo "   1. Run app-deploy.sh as regular user to start applications"
echo "   2. Start system services: sudo launchctl load /Library/LaunchDaemons/dev.sparkify.nginx.plist"
echo "   3. Start tunnel service: sudo launchctl load /Library/LaunchDaemons/com.cloudflare.sparkify.plist"