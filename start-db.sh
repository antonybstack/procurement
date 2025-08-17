#!/bin/bash

# This script starts the database services while preserving existing data.
# It stops existing containers but PRESERVES the persistent data volume
# to maintain data across restarts.
# Now includes automatic pgai setup for TigerData migration.

# Load environment variables from .env
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
    echo "📦 Loaded .env"
else
    echo "⚠️  .env not found"
fi


echo "🚀 Starting database services..."

# Stop existing containers defined in docker-compose.db.yml
# Note: We do NOT use the -v flag to preserve the data volume
echo "🛑 Stopping existing DB containers (preserving data)..."
docker-compose -f docker-compose.db.yml down

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Start Database Services (PostgreSQL)
echo "🚀 Starting fresh database services..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Start the database services. Data will be preserved from previous runs.
echo "📦 Starting PostgreSQL..."
docker-compose -f docker-compose.db.yml up -d

# Check if services started successfully
if [ $? -eq 0 ]; then
    echo "✅ Database services started successfully!"
    echo "📊 PostgreSQL: localhost:5432"
    echo "   - Password: postgres_password"
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

# Auto-setup pgai using dedicated setup script
echo ""
echo "🤖 Setting up database AI stack..."
if [ -f setup-pgai.sh ]; then
    echo "🔄 Running comprehensive pgai setup..."
    ./setup-pgai.sh
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "🚀 Database AI stack is ready!"
        echo "📊 PostgreSQL: localhost:5432"
        echo "🤖 Vectorizer worker: monitoring for new data"
        echo "🔍 Vector search: ready for queries"
    else
        echo "❌ pgai setup failed - please check the output above"
        exit 1
    fi
else
    echo "❌ setup-pgai.sh not found - please run it manually after database is ready"
    echo "📊 PostgreSQL: localhost:5432 (database only)"
fi
