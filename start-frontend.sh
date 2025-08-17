#!/bin/bash

# Start Angular Frontend service
echo "Starting Angular Frontend service..."

# Check if the observability network exists
if ! docker network ls | grep -q "procurement_observability"; then
    echo "Creating procurement_observability network..."
    docker network create procurement_observability
fi

# Build and start the frontend service (force rebuild)
docker-compose -f docker-compose.frontend.yml up -d --build

# Wait a moment for the container to start
sleep 5

# Check if the service is running
if docker ps | grep -q "procurement_frontend"; then
    echo "✅ Angular Frontend service is running on http://localhost:4200"
    echo "Health check: http://localhost:4200/health"
else
    echo "❌ Failed to start Angular Frontend service"
    exit 1
fi 