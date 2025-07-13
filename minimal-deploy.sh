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

# Step 3: Clone repo and deploy minimal stack
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
add_ufw_rule 443
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
# Start Postgres and pgAdmin
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

# Start Nginx proxy with HTTPS
if [ -f docker-compose.nginx.yml ]; then
  docker-compose -f docker-compose.nginx.yml up -d
fi

echo "\n====================================="
echo "Minimal stack deployed with HTTPS support!"
echo "- Frontend:      https://sparkify.dev"
echo "- API:           https://sparkify.dev/api"
echo "- API Swagger:   https://sparkify.dev/api/swagger"
echo ""
echo "Using Let's Encrypt certificates for secure HTTPS"
echo "=====================================" 