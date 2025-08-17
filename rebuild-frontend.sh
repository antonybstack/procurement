#!/bin/bash

# Rebuild Angular Frontend with fresh build
echo "Rebuilding Angular Frontend service..."

# Stop the existing container
echo "Stopping existing frontend container..."
docker-compose -f docker-compose.frontend.yml down

# Remove the image to force a complete rebuild
echo "Removing existing frontend image..."
docker rmi procurement_frontend 2>/dev/null || true

# Check if the observability network exists
if ! docker network ls | grep -q "procurement_observability"; then
    echo "Creating procurement_observability network..."
    docker network create procurement_observability
fi

# Build and start with no cache
echo "Building and starting frontend service..."
docker-compose -f docker-compose.frontend.yml build --no-cache
docker-compose -f docker-compose.frontend.yml up -d

# Wait a moment for the container to start
sleep 10

# Check if the service is running
if docker ps | grep -q "procurement_frontend"; then
    echo "✅ Angular Frontend service is running on http://localhost:4200"
    echo "Health check: http://localhost:4200/health"
else
    echo "❌ Failed to start Angular Frontend service"
    docker-compose -f docker-compose.frontend.yml logs
    exit 1
fi