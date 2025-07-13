#!/bin/bash

# Minimal deployment script for Digital Ocean
# Only sets up Postgres, .NET API, and Angular frontend

set -e

# Step 1: Update and upgrade system
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

sudo ufw allow 22
sudo ufw allow 443
sudo ufw allow 4200
sudo ufw allow 5001
sudo ufw allow 8080
sudo ufw enable

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

echo "\n====================================="
echo "Minimal stack deployed with Nginx proxy!"
echo "- Frontend:      http://<your-ip>:8080/app"
echo "- API:           http://<your-ip>:8080/api"
echo "- pgAdmin:       http://<your-ip>:8080/pg"
echo "- Health check:  http://<your-ip>:8080/health"
echo ""
echo "Port 80 is now free for Let's Encrypt setup!"
echo "=====================================" 