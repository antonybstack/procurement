#!/bin/bash

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Start API Service
echo "🚀 Starting API service..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Check if database services are running
echo "🔍 Checking if database services are running..."
if ! docker-compose -f docker-compose.db.yml ps postgres | grep -q "Up"; then
    echo "❌ Database services are not running. Please run 'start-db.sh' first."
    exit 1
fi

# Check if PostgreSQL is healthy
echo "⏳ Checking PostgreSQL health..."
timeout=30
counter=0
while [ $counter -lt $timeout ]; do
    if docker-compose -f docker-compose.db.yml exec -T postgres pg_isready -U postgres -d myapp > /dev/null 2>&1; then
        echo "✅ PostgreSQL is healthy!"
        break
    fi
    sleep 2
    counter=$((counter + 2))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ PostgreSQL is not healthy. Please check database services."
    exit 1
fi

# Start the API service
echo "📦 Starting Procurement API..."
docker-compose -f docker-compose.api.yml up -d --build

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
    docker-compose -f docker-compose.api.yml logs procurement-api --tail=20
    exit 1
fi

echo "🎉 API service is ready!" 