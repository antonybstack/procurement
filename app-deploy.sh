#!/bin/bash

# Application deployment script for Procurement Management System
# This script deploys user-level applications and must be run as regular user
# Usage: ./app-deploy.sh

set -e

if [ "$EUID" -eq 0 ]; then
    echo "❌ This script should NOT be run with sudo"
    echo "Usage: ./app-deploy.sh"
    exit 1
fi

echo "🚀 Application Deployment"
echo "========================"
echo ""

DOMAIN="sparkify.dev"

# Step 1: Install required tools via Homebrew
echo "📦 Step 1: Installing required tools..."

# Check if Homebrew is installed
if ! command -v brew &> /dev/null; then
    echo "❌ Homebrew is not installed. Please install it first:"
    echo "   /bin/bash -c \"\$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)\""
    exit 1
fi

# Install nginx if not already installed
if ! command -v nginx &> /dev/null; then
    echo "Installing nginx..."
    brew install nginx
else
    echo "✅ nginx already installed"
fi

# Install mkcert for local SSL certificates
if ! command -v mkcert &> /dev/null; then
    echo "Installing mkcert for local SSL certificates..."
    brew install mkcert
    mkcert -install
else
    echo "✅ mkcert already installed"
fi

# Install cloudflared for tunnel if not already installed
if ! command -v cloudflared &> /dev/null; then
    echo "Installing cloudflared for Cloudflare tunnel..."
    brew install cloudflared
else
    echo "✅ cloudflared already installed"
fi

# Step 2: Create local SSL certificates
echo ""
echo "🔒 Step 2: Creating local SSL certificates..."

SSL_DIR="./ssl"
mkdir -p "$SSL_DIR"

if [ ! -f "$SSL_DIR/$DOMAIN.pem" ]; then
    echo "Creating SSL certificate for $DOMAIN..."
    cd "$SSL_DIR"
    mkcert "$DOMAIN"
    cd ..
    echo "✅ SSL certificate created in $SSL_DIR"
else
    echo "✅ SSL certificate already exists in $SSL_DIR"
fi

# Step 3: Create Docker networks
echo ""
echo "🐳 Step 3: Setting up Docker networks..."

docker network create postgres_network 2>/dev/null || echo "✅ postgres_network already exists"
docker network create procurement_observability 2>/dev/null || echo "✅ procurement_observability already exists"

# Step 4: Start application services
echo ""
echo "🚀 Step 4: Starting application services..."

# Start Postgres
if [ -f docker-compose.db.yml ]; then
    echo "Starting Postgres..."
    docker-compose -f docker-compose.db.yml up -d
    echo "✅ Postgres started"
fi

# Start API (with user environment variables)
if [ -f docker-compose.api.yml ]; then
    echo "Starting API..."
    docker-compose -f docker-compose.api.yml up -d
    echo "✅ API started"
fi

# Start Frontend
if [ -f docker-compose.frontend.yml ]; then
    echo "Starting Frontend..."
    docker-compose -f docker-compose.frontend.yml up -d
    echo "✅ Frontend started"
fi

# Step 5: Wait for services and health checks
echo ""
echo "⏳ Step 5: Waiting for services to be ready..."

sleep 10

# Health check function
check_service() {
    local url=$1
    local name=$2
    
    if curl -s -o /dev/null -w "%{http_code}" "$url" | grep -q "200\|302"; then
        echo "✅ $name is responding"
        return 0
    else
        echo "❌ $name is not responding at $url"
        return 1
    fi
}

# Application health checks
echo "🔍 Running application health checks..."
check_service "http://localhost:5001/health" "API" || echo "⚠️  API health check failed (service may still be starting)"
check_service "http://localhost:4200" "Frontend" || echo "⚠️  Frontend health check failed (service may still be starting)"

echo ""
echo "🎉 Application deployment complete!"
echo "=================================="
echo ""
echo "🔗 Direct service access:"
echo "   - API:           http://localhost:5001"
echo "   - API Swagger:   http://localhost:5001/swagger"
echo "   - Frontend:      http://localhost:4200"
echo "   - PostgreSQL:    localhost:5432 (password: admin_password)"
echo ""
echo "📝 Next steps:"
echo "   1. Ensure system services are running:"
echo "      sudo launchctl load /Library/LaunchDaemons/dev.sparkify.nginx.plist"
echo "      sudo launchctl load /Library/LaunchDaemons/com.cloudflare.sparkify.plist"
echo ""
echo "   2. Start system services:"
echo "      sudo launchctl start dev.sparkify.nginx"
echo "      sudo launchctl start com.cloudflare.sparkify"
echo ""
echo "   3. Test full stack:"
echo "      curl https://sparkify.dev/api/health"
echo ""
echo "📦 Application management:"
echo "   - View services: docker ps"
echo "   - Stop services: ./stop.sh"
echo "   - Restart API:   docker-compose -f docker-compose.api.yml restart"