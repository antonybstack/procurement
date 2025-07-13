#!/bin/bash

# Rebuild Angular Frontend service
echo "🔨 Rebuilding Angular Frontend service..."

# Stop the service first
docker-compose -f docker-compose.frontend.yml down

# Remove the old image
docker rmi procurement_frontend 2>/dev/null || true

# Rebuild and start the service
docker-compose -f docker-compose.frontend.yml up -d --build

# Wait a moment for the container to start
sleep 5

# Check if the service is running
if docker ps | grep -q "procurement_frontend"; then
    echo "✅ Angular Frontend service rebuilt and running on http://localhost:4200"
else
    echo "❌ Failed to rebuild Angular Frontend service"
    exit 1
fi 