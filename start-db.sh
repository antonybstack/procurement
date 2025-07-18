#!/bin/bash

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw postgres_network; then
  echo "🔧 Creating external network: postgres_network"
  docker network create postgres_network
fi

# Start Database Services (PostgreSQL + pgAdmin)
echo "🚀 Starting database services..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Start the database services
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
    if docker-compose -f docker-compose.db.yml exec -T postgres pg_isready -U postgres -d myapp > /dev/null 2>&1; then
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

echo "🎉 Database services are ready!" 