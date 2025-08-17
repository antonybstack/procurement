#!/bin/bash

# Mac Studio M1 deployment orchestrator for Procurement Management System
# This script coordinates system setup (requiring sudo) and app deployment (user-level)

set -e

echo "🍎 Mac Studio M1 Deployment Orchestrator"
echo "========================================"
echo ""

# Check if we're running as root - we should NOT be
if [ "$EUID" -eq 0 ]; then
    echo "❌ This script should NOT be run with sudo"
    echo "   It will request sudo privileges when needed"
    echo ""
    echo "Usage: ./mac-deploy.sh"
    exit 1
fi

DOMAIN="sparkify.dev"

echo "🔍 Running deployment for domain: $DOMAIN"
echo "🔍 User: $(whoami)"
echo "🔍 Working directory: $(pwd)"
echo ""

# Phase 1: System Infrastructure Setup (requires sudo)
echo "=================="
echo "Phase 1: System Infrastructure Setup"
echo "=================="
echo ""
echo "This phase requires administrator privileges to configure:"
echo "  - System hosts file"
echo "  - SSL certificates in system directory"  
echo "  - nginx and cloudflared LaunchDaemons"
echo "  - Power management settings"
echo ""

if [ -f "./system-setup.sh" ]; then
    echo "🔧 Running system setup (will prompt for sudo password)..."
    sudo ./system-setup.sh
    echo ""
    echo "✅ System infrastructure setup complete"
else
    echo "❌ system-setup.sh not found in current directory"
    exit 1
fi

echo ""
echo "=================="
echo "Phase 2: Application Deployment"  
echo "=================="
echo ""
echo "This phase runs as regular user and configures:"
echo "  - User-level tool installation"
echo "  - Local SSL certificates"
echo "  - Docker containers and networks"
echo "  - Application services"
echo ""

if [ -f "./app-deploy.sh" ]; then
    echo "🚀 Running application deployment..."
    ./app-deploy.sh
    echo ""
    echo "✅ Application deployment complete"
else
    echo "❌ app-deploy.sh not found in current directory"
    exit 1
fi

echo ""
echo "=================="
echo "Phase 3: System Service Activation"
echo "=================="
echo ""
echo "🔄 Starting system services (requires sudo)..."

# Load and start nginx LaunchDaemon
if sudo launchctl load /Library/LaunchDaemons/dev.sparkify.nginx.plist 2>/dev/null; then
    echo "✅ nginx LaunchDaemon loaded"
else
    echo "⚠️  nginx LaunchDaemon already loaded or failed to load"
fi

if sudo launchctl start dev.sparkify.nginx 2>/dev/null; then
    echo "✅ nginx system service started"
else
    echo "⚠️  nginx system service already running or failed to start"
fi

# Load and start cloudflared LaunchDaemon  
if sudo launchctl load /Library/LaunchDaemons/com.cloudflare.sparkify.plist 2>/dev/null; then
    echo "✅ cloudflared LaunchDaemon loaded"
else
    echo "⚠️  cloudflared LaunchDaemon already loaded or failed to load"
fi

if sudo launchctl start com.cloudflare.sparkify 2>/dev/null; then
    echo "✅ cloudflared system service started"
else
    echo "⚠️  cloudflared system service already running or failed to start"
fi

echo ""
echo "⏳ Waiting for system services to stabilize..."
sleep 5

echo ""
echo "=================="
echo "Phase 4: Final Health Checks"
echo "=================="
echo ""

# Health check function
check_service() {
    local url=$1
    local name=$2
    local timeout_seconds=${3:-10}
    
    echo "Testing $name at $url..."
    local response_code=$(curl -s -o /dev/null -w "%{http_code}" --max-time "$timeout_seconds" "$url" 2>/dev/null)
    
    if [[ "$response_code" =~ ^(200|301|302)$ ]]; then
        echo "✅ $name is responding (HTTP $response_code)"
        return 0
    else
        echo "❌ $name is not responding at $url (HTTP $response_code)"
        return 1
    fi
}

echo "🔍 Testing full stack connectivity..."

# Test direct services (non-fatal)
check_service "http://localhost:5001/health" "API (direct)" 10 || echo "⚠️  API may still be starting"
check_service "http://localhost:4200" "Frontend (direct)" 5 || echo "⚠️  Frontend may still be starting"

# Test through nginx (local)
echo ""
echo "🌐 Testing through nginx (local)..."
check_service "http://localhost/api/health" "API (via nginx)" 5 || echo "⚠️  nginx may not be fully started yet"
check_service "http://localhost" "Frontend (via nginx)" 5 || echo "⚠️  nginx may not be fully started yet"

# Test through public domain (if tunnel is working)
echo ""
echo "🌐 Testing public access (may take a moment for tunnel to connect)..."
check_service "https://sparkify.dev/api/health" "API (public)" 10 || echo "⚠️  Cloudflare tunnel may still be connecting"
check_service "https://sparkify.dev" "Frontend (public)" 5 || echo "⚠️  Cloudflare tunnel may still be connecting"

echo ""
echo "🎉 Mac Studio M1 Deployment Complete!"
echo "===================================="
echo ""
echo "🌐 Access your application:"
echo "   - HTTPS: https://$DOMAIN"
echo "   - HTTP:  http://$DOMAIN (redirects to HTTPS)"
echo ""
echo "🔗 Direct service access:"
echo "   - API:           http://localhost:5001"
echo "   - API Swagger:   http://localhost:5001/swagger"
echo "   - Frontend:      http://localhost:4200"
echo "   - PostgreSQL:    localhost:5432 (password: admin_password)"
echo ""
echo "📋 System Status Commands:"
echo ""
echo "🌐 nginx system service:"
echo "   - Status: sudo launchctl list | grep nginx"
echo "   - Stop:   sudo launchctl stop dev.sparkify.nginx"
echo "   - Start:  sudo launchctl start dev.sparkify.nginx"
echo "   - Logs:   tail -f $(brew --prefix)/var/log/nginx/error.log"
echo ""
echo "☁️ Cloudflare tunnel service:"
echo "   - Status: sudo launchctl list | grep cloudflare"
echo "   - Stop:   sudo launchctl stop com.cloudflare.sparkify"
echo "   - Start:  sudo launchctl start com.cloudflare.sparkify"
echo "   - Logs:   tail -f /var/log/cloudflared.log"
echo ""
echo "📦 Application services:"
echo "   - Status: docker ps"
echo "   - Stop:   ./stop.sh"
echo "   - Restart API: docker-compose -f docker-compose.api.yml restart"
echo ""
echo "⚡ Power management:"
echo "   - Current settings: pmset -g"
echo "   - Display sleep is disabled for tunnel stability"
echo ""
echo "🔒 SSL Certificates:"
echo "   - System location: /etc/ssl/sparkify/$DOMAIN.pem"
echo "   - Local backup: $(pwd)/ssl/$DOMAIN.pem"
echo ""
echo "☁️ Cloudflare Configuration:"
echo "   - System config: /etc/cloudflared/config.yml"
echo "   - System credentials: /etc/cloudflared/cert.pem"
echo ""
echo "🚀 All services are configured to start automatically on boot!"
echo "   Both nginx and cloudflared will persist through screen locks and reboots."