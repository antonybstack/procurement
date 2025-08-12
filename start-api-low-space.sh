#!/bin/bash

# Low-space variant of start-api.sh
echo "🚀 Starting API service (low-space mode)..."

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Check if database services are running
echo "🔍 Checking if database services are running..."
if ! docker-compose -f docker-compose.db.yml ps postgres | grep -q "Up"; then
    echo "❌ Database services are not running. Please run 'start-db.sh' first."
    exit 1
fi

# Clear space before build
echo "🧹 Clearing space before build..."
docker system prune -f
rm -rf /tmp/NuGetScratch* 2>/dev/null || true

# Build with reduced cache and parallel restrictions
echo "📦 Building with space optimizations..."
DOCKER_BUILDKIT=1 docker-compose -f docker-compose.api.lowspace.yml build

# Start the service
docker-compose -f docker-compose.api.lowspace.yml up -d

# Check if service started successfully
if [ $? -eq 0 ]; then
    echo "✅ API service started successfully!"
    echo "🌐 API: http://localhost:5001"
    echo "📚 Swagger: http://localhost:5001/swagger"
else
    echo "❌ Failed to start API service"
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
    docker-compose -f docker-compose.api.lowspace.yml logs procurement-api --tail=20
    exit 1
fi

echo "🎉 API service is ready!"
