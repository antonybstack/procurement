#!/bin/bash

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Stop and remove existing containers
echo "🛑 Stopping existing API containers..."
docker-compose -f docker-compose.api.yml down

# Remove the image to force a complete rebuild
echo "🗑️ Removing existing API image..."
docker rmi procurement_procurement-api 2>/dev/null || true

# Rebuild and start the API service
echo "🔨 Rebuilding and starting API service..."
docker-compose -f docker-compose.api.yml up -d --build --force-recreate

# Check if service started successfully
if [ $? -eq 0 ]; then
    echo "✅ API service rebuilt and started successfully!"
    echo "🌐 API: http://localhost:5001"
    echo "📚 Swagger: http://localhost:5001/swagger"
else
    echo "❌ Failed to rebuild and start API service"
    exit 1
fi

# Wait for API to be healthy
echo "⏳ Waiting for API to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    if curl -f http://localhost:5001/health/ready > /dev/null 2>&1; then
        echo "✅ API is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ API failed to become ready within $timeout seconds"
    echo "📋 Checking API logs..."
    docker-compose -f docker-compose.api.yml logs procurement-api --tail=20
    exit 1
fi

echo "🎉 API service rebuilt and ready!" 