#!/bin/bash

# This script ensures a clean start for the database services every time.
# It tears down existing containers and REMOVES the persistent data volume
# before starting the services again, forcing a re-initialization from the
# init-scripts directory.

echo "🧹 Preparing for a clean database start..."

# Stop and remove existing containers defined in docker-compose.db.yml
# The `-v` flag is crucial as it removes named volumes associated with the services.
echo "🛑 Stopping and removing existing DB containers and volumes..."
docker-compose -f docker-compose.db.yml down -v

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Start Database Services (PostgreSQL + pgAdmin)
echo "🚀 Starting fresh database services..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Start the database services. The previous 'down -v' ensures this is a fresh start.
echo "📦 Starting PostgreSQL and pgAdmin..."
docker-compose -f docker-compose.db.yml up -d

# Check if services started successfully
if [ $? -eq 0 ]; then
    echo "✅ Database services started successfully!"
    echo "📊 PostgreSQL: localhost:5432"
    echo "🗄️  pgAdmin: http://localhost:8080"
    echo "   - Email: admin@example.com"
    echo "   - Password: admin_password"
else
    echo "❌ Failed to start database services"
    exit 1
fi

# Wait for PostgreSQL to be healthy
echo "⏳ Waiting for PostgreSQL to be ready..."
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    # Use 'docker compose' ps to check the health status directly
    HEALTH_STATUS=$(docker-compose -f docker-compose.db.yml ps -q postgres | xargs docker inspect -f '{{.State.Health.Status}}')
    if [ "$HEALTH_STATUS" == "healthy" ]; then
        echo "✅ PostgreSQL is ready!"
        break
    fi
    sleep 2
    counter=$((counter + 2))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ PostgreSQL failed to become ready within $timeout seconds"
    exit 1
fi

echo "🎉 Database services are ready for connections!"
