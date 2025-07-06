#!/bin/bash

# Stop all services
echo "🛑 Stopping all services..."

# Stop API service
echo "📦 Stopping API service..."
docker-compose -f docker-compose.api.yml down

# Stop database services
echo "📊 Stopping database services..."
docker-compose -f docker-compose.db.yml down

echo "✅ All services stopped!" 