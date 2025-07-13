#!/bin/bash

# Stop all services
echo "🛑 Stopping all services..."

# Stop API service
echo "📦 Stopping API service..."
docker-compose -f docker-compose.api.yml down

# Stop Frontend service
echo "🎨 Stopping Frontend service..."
docker-compose -f docker-compose.frontend.yml down

# Stop Elastic services
echo "🔍 Stopping Elastic services..."
docker-compose -f docker-compose.elastic.yml down

# Stop database services
echo "📊 Stopping database services..."
docker-compose -f docker-compose.db.yml down

# Stop Grafana services
echo "📊 Stopping Grafana services..."
docker-compose -f docker-compose.grafana.yml down

echo "✅ All services stopped!" 