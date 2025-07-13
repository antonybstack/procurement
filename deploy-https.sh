#!/bin/bash

# Deploy HTTPS-enabled configuration to server
echo "Deploying HTTPS-enabled configuration..."

# Stop existing nginx proxy
docker-compose -f docker-compose.nginx.yml down

# Start nginx proxy with HTTPS
docker-compose -f docker-compose.nginx.yml up -d

echo "HTTPS deployment complete!"
echo "Access your services at:"
echo "- Frontend:      https://sparkify.dev"
echo "- API:           https://sparkify.dev/api"
echo "- API Swagger:   https://sparkify.dev/api/swagger"
echo ""
echo "Using Let's Encrypt certificates for secure HTTPS" 