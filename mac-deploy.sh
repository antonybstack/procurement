#!/bin/bash

# Mac Studio M1 deployment script for local development
# Sets up Postgres, .NET API, and Angular frontend with nginx reverse proxy

set -e

echo "🍎 Starting Mac Studio M1 deployment..."
echo "======================================"

# Step 1: Install required tools via Homebrew
echo ""
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

# Step 2: Set up local domain in /etc/hosts
echo ""
echo "🌐 Step 2: Configuring local domain..."

DOMAIN="sparkify.dev"
HOSTS_ENTRY="127.0.0.1 $DOMAIN"

if ! grep -q "$DOMAIN" /etc/hosts; then
    echo "Adding $DOMAIN to /etc/hosts (requires sudo)..."
    echo "$HOSTS_ENTRY" | sudo tee -a /etc/hosts > /dev/null
    echo "✅ Added $DOMAIN to /etc/hosts"
else
    echo "✅ $DOMAIN already exists in /etc/hosts"
fi

# Step 3: Create SSL certificates for local development
echo ""
echo "🔒 Step 3: Creating local SSL certificates..."

SSL_DIR="./ssl"
mkdir -p "$SSL_DIR"

if [ ! -f "$SSL_DIR/$DOMAIN.pem" ]; then
    echo "Creating SSL certificate for $DOMAIN..."
    cd "$SSL_DIR"
    mkcert "$DOMAIN"
    cd ..
    echo "✅ SSL certificate created"
else
    echo "✅ SSL certificate already exists"
fi

# Step 4: Create Docker networks
echo ""
echo "🐳 Step 4: Setting up Docker networks..."

docker network create postgres_network 2>/dev/null || echo "✅ postgres_network already exists"
docker network create procurement_observability 2>/dev/null || echo "✅ procurement_observability already exists"

# Step 5: Start minimal services
echo ""
echo "🚀 Step 5: Starting minimal services..."

# Start Postgres
if [ -f docker-compose.db.yml ]; then
    echo "Starting Postgres..."
    docker-compose -f docker-compose.db.yml up -d
    echo "✅ Postgres started"
fi

# Start API
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

# Step 6: Configure nginx for reverse proxy
echo ""
echo "⚙️  Step 6: Configuring nginx..."

# Determine nginx configuration directory (M1 Mac with Homebrew)
if [ -d "/opt/homebrew/etc/nginx" ]; then
    NGINX_DIR="/opt/homebrew/etc/nginx"
elif [ -d "/usr/local/etc/nginx" ]; then
    NGINX_DIR="/usr/local/etc/nginx"
else
    echo "❌ Could not find nginx configuration directory"
    exit 1
fi

# Check if our custom nginx config exists, if not copy the template
if [ ! -f "$NGINX_DIR/nginx.conf.procurement" ]; then
    echo "Creating nginx configuration from template..."
    if [ -f "./nginx-mac.conf" ]; then
        # Update paths in the template to use absolute paths
        sed -e "s|/Users/antbly/dev/procurement|$(pwd)|g" \
            -e "s|HOMEBREW_PREFIX|$(brew --prefix)|g" \
            ./nginx-mac.conf > "$NGINX_DIR/nginx.conf.procurement"
    else
        echo "❌ nginx-mac.conf template not found"
        exit 1
    fi
    echo "✅ nginx configuration template created"
else
    echo "✅ nginx configuration already exists"
fi

# Backup original nginx.conf if it exists and isn't already backed up
if [ -f "$NGINX_DIR/nginx.conf" ] && [ ! -f "$NGINX_DIR/nginx.conf.original" ]; then
    cp "$NGINX_DIR/nginx.conf" "$NGINX_DIR/nginx.conf.original"
    echo "✅ Original nginx.conf backed up"
fi

# Use our procurement-specific configuration
cp "$NGINX_DIR/nginx.conf.procurement" "$NGINX_DIR/nginx.conf"

echo "✅ nginx configuration created"

# Step 7: Create log directory and configure nginx for privileged ports
echo ""
echo "🔄 Step 7: Configuring nginx for ports 80/443..."

# Create log directory if it doesn't exist
mkdir -p "$(brew --prefix)/var/log/nginx"

# Replace the current config with our standard ports version
sed -e "s|/Users/antbly/dev/procurement|$(pwd)|g" \
    -e "s|HOMEBREW_PREFIX|$(brew --prefix)|g" \
    ./nginx.conf > "$NGINX_DIR/nginx.conf"

# Test nginx configuration
if nginx -t; then
    echo "✅ nginx configuration is valid"
else
    echo "❌ nginx configuration is invalid"
    exit 1
fi

# Stop current nginx service
brew services stop nginx 2>/dev/null || true

# Create a system-level LaunchDaemon for nginx with privileged ports
echo "Creating system LaunchDaemon for nginx (requires sudo)..."
echo "You may be prompted for your password to allow nginx to use ports 80/443"

# Create the LaunchDaemon plist
cat > /tmp/nginx.plist << 'EOF'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>dev.sparkify.nginx</string>
    <key>ProgramArguments</key>
    <array>
        <string>NGINX_PATH</string>
        <string>-g</string>
        <string>daemon off;</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>WorkingDirectory</key>
    <string>HOMEBREW_PREFIX</string>
    <key>StandardOutPath</key>
    <string>HOMEBREW_PREFIX/var/log/nginx/access.log</string>
    <key>StandardErrorPath</key>
    <string>HOMEBREW_PREFIX/var/log/nginx/error.log</string>
</dict>
</plist>
EOF

# Replace placeholders in the plist
sed -e "s|NGINX_PATH|$(brew --prefix)/bin/nginx|g" \
    -e "s|HOMEBREW_PREFIX|$(brew --prefix)|g" \
    /tmp/nginx.plist > /tmp/nginx-final.plist

echo "Starting nginx on standard ports 80/443 (requires sudo)..."

# Use the standard ports config
sed -e "s|/Users/antbly/dev/procurement|$(pwd)|g" \
    -e "s|HOMEBREW_PREFIX|$(brew --prefix)|g" \
    ./nginx.conf > "$NGINX_DIR/nginx.conf"

# Enable standard ports
./enable-standard-ports.sh

echo "✅ nginx started on ports 80/443"

# Step 8: Wait for services to be ready
echo ""
echo "⏳ Step 8: Waiting for services to be ready..."

sleep 5

# Step 9: Health checks
echo ""
echo "🔍 Step 9: Running health checks..."

# Check if services are responding
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

# Health checks (non-fatal - services might still be starting)
check_service "http://localhost:5001/health" "API" || echo "⚠️  API health check failed (service may still be starting)"
check_service "http://localhost:4200" "Frontend" || echo "⚠️  Frontend health check failed (service may still be starting)"

echo ""
echo "🎉 Mac deployment complete!"
echo "=========================="
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
echo "📝 Useful commands:"
echo "   - nginx status:  brew services list | grep nginx"
echo "   - nginx stop:    brew services stop nginx"
echo "   - nginx restart: brew services restart nginx"
echo "   - nginx logs:    tail -f $(brew --prefix)/var/log/nginx/error.log"
echo "   - View services: docker ps"
echo "   - Stop services: ./stop.sh"
echo ""
echo "🔒 SSL Certificate: Self-signed certificate installed for local development"
echo "   Certificate location: $(pwd)/ssl/$DOMAIN.pem"