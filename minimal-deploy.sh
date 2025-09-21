#!/bin/bash

# Minimal deployment script for Digital Ocean
# Only sets up Postgres, .NET API, and Angular frontend

# chmod +x ~/minimal-deploy.sh

set -e

# Step 1: Update and upgrade system
export DEBIAN_FRONTEND=noninteractive
export UCF_FORCE_CONFNEW=1
apt update && apt upgrade -y

# Step 2: Install Docker and Docker Compose
apt install -y docker.io docker-compose
systemctl enable --now docker

# Step 3: Configure SSH for remote development (idempotent)
echo "Configuring SSH for remote development..."

# Function to update SSH config setting
update_ssh_config() {
    local setting=$1
    local value=$2
    local file="/etc/ssh/sshd_config"
    
    # Check if setting already exists with correct value
    if grep -q "^${setting}[[:space:]]*${value}$" "$file"; then
        echo "SSH setting $setting already set to $value"
        return 0
    fi
    
    # Remove existing setting if it exists
    sed -i "/^${setting}[[:space:]]/d" "$file"
    
    # Add new setting
    echo "$setting $value" >> "$file"
    echo "Updated SSH setting: $setting $value"
}

# Backup original SSH config only if not already backed up today
BACKUP_FILE="/etc/ssh/sshd_config.backup.$(date +%Y%m%d)"
if [ ! -f "$BACKUP_FILE" ]; then
    cp /etc/ssh/sshd_config "$BACKUP_FILE"
    echo "Created SSH config backup: $BACKUP_FILE"
else
    echo "SSH config backup already exists for today: $BACKUP_FILE"
fi

# Ensure required SSH settings for remote development
update_ssh_config "AllowTcpForwarding" "yes"
update_ssh_config "GatewayPorts" "yes"
update_ssh_config "PermitTunnel" "yes"
update_ssh_config "X11Forwarding" "yes"
update_ssh_config "PubkeyAuthentication" "yes"
update_ssh_config "PasswordAuthentication" "no"
update_ssh_config "PermitRootLogin" "yes"
update_ssh_config "TCPKeepAlive" "yes"
update_ssh_config "ClientAliveInterval" "60"
update_ssh_config "ClientAliveCountMax" "3"
update_ssh_config "Compression" "yes"
update_ssh_config "MaxSessions" "10"
update_ssh_config "MaxStartups" "10:30:60"

# Restart SSH service
systemctl daemon-reload
systemctl restart ssh.socket
systemctl restart ssh
echo "SSH configured for remote development"

# Step 4: Clone repo and deploy minimal stack
mkdir -p /opt/procurement
cd /opt/procurement

if [ ! -d .git ]; then
  git clone https://github.com/antonybstack/procurement .
fi

chmod +x *.sh

# Create required Docker networks (idempotent)
docker network create postgres_network 2>/dev/null || true
docker network create procurement_observability 2>/dev/null || true

# Configure firewall rules (idempotent)
echo "Configuring firewall rules..."

# Function to add UFW rule if it doesn't exist
add_ufw_rule() {
    local port=$1
    local protocol=${2:-tcp}
    
    # Check if rule already exists
    if ! ufw status numbered | grep -q "ALLOW.*$port"; then
        echo "Adding UFW rule for port $port"
        ufw allow $port/$protocol
    else
        echo "UFW rule for port $port already exists"
    fi
}

# Add rules only if they don't exist
add_ufw_rule 22
add_ufw_rule 80
## Do not open 443; TLS terminates at Cloudflare
add_ufw_rule 4200
add_ufw_rule 5001
add_ufw_rule 8080

# Enable UFW if not already enabled
if ! ufw status | grep -q "Status: active"; then
    echo "Enabling UFW firewall"
    ufw --force enable
else
    echo "UFW firewall already enabled"
fi

# Start only the minimal services
# Start Postgres
if [ -f docker-compose.db.yml ]; then
  docker-compose -f docker-compose.db.yml up -d
fi
# Start API
if [ -f docker-compose.api.yml ]; then
  docker-compose -f docker-compose.api.yml up -d
fi
# Start Frontend
if [ -f docker-compose.frontend.yml ]; then
  docker-compose -f docker-compose.frontend.yml up -d
fi

# Configure host Nginx for reverse proxy (HTTP origin; TLS via Cloudflare)
echo "Configuring host Nginx for reverse proxy (HTTP-only origin)..."

# Install Nginx if not already installed
if ! command -v nginx &> /dev/null; then
    apt install -y nginx
    systemctl enable nginx
fi

# Create Nginx configuration for reverse proxy (port 80 only)
cat > /etc/nginx/sites-available/procurement << 'EOF'
server {
    listen 80;
    server_name sparkify.dev;

    # Frontend (Angular app)
    location / {
        proxy_pass http://localhost:4200;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }

    # API
    location /api/ {
        proxy_pass http://localhost:5001/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
EOF

# Enable the site
ln -sf /etc/nginx/sites-available/procurement /etc/nginx/sites-enabled/
rm -f /etc/nginx/sites-enabled/default

# Test Nginx configuration
nginx -t

# Restart Nginx
systemctl restart nginx
echo "Host Nginx configured for reverse proxy"

echo "\n====================================="
echo "Minimal stack deployed (HTTP origin; TLS via Cloudflare)"
echo "- Public Frontend: https://sparkify.dev"
echo "- Public API:      https://sparkify.dev/api"
echo "- Swagger:         https://sparkify.dev/api/swagger"
echo "=====================================" 
