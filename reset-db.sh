#!/bin/bash

# This script resets the database by removing all data and starting fresh.
# WARNING: This will delete ALL existing data in the database!
# Use this only for development/testing when you need a clean slate.

echo "⚠️  WARNING: This will delete ALL database data!"
echo "This action cannot be undone."
echo ""
read -p "Are you sure you want to reset the database? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Database reset cancelled."
    exit 0
fi

echo "🧹 Resetting database (removing all data)..."

# Stop and remove existing containers AND volumes
echo "🛑 Stopping containers and removing data volumes..."
docker-compose -f docker-compose.db.yml down -v

# Remove the volumes manually to ensure complete cleanup
echo "🗑️  Removing data volumes..."
docker volume rm postgres_data pgadmin_data 2>/dev/null || true

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

# Start the database services with fresh volumes
echo "📦 Starting PostgreSQL and pgAdmin with fresh data..."
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

echo "🎉 Database has been reset and is ready for connections!"
echo "📝 Note: All previous data has been removed." 