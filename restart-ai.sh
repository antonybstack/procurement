#!/bin/bash

# Restart AI services while preserving data
echo "🔄 Restarting AI services (preserving data)..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Check if postgres service is running
if ! docker ps | grep -q "postgres_db"; then
    echo "❌ Database services are not running. Please run 'start-db.sh' first."
    exit 1
fi

# Stop AI services (preserving any data)
echo "🛑 Stopping AI services..."
docker-compose -f docker-compose.ai.mini.yml down

# Start AI services
echo "🚀 Starting AI services..."
docker-compose -f docker-compose.ai.mini.yml up -d

# Check if service started successfully
if [ $? -eq 0 ]; then
    echo "✅ AI services restarted successfully!"
else
    echo "❌ Failed to restart AI services"
    exit 1
fi

# Wait for the service to be healthy
echo "⏳ Waiting for AI service to be ready..."
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    if curl -f http://localhost:8000/health > /dev/null 2>&1; then
        echo "✅ AI services are ready!"
        break
    fi
    sleep 2
    counter=$((counter + 2))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ AI services failed to become ready within $timeout seconds"
    echo "📋 Checking AI service logs..."
    docker-compose -f docker-compose.ai.mini.yml logs ai-recommendation-service --tail=20
    exit 1
fi

echo "🎉 AI services restarted and ready!"
echo "🌐 Service URL: http://localhost:8000"
echo "📊 Health Check: http://localhost:8000/health"
echo "📝 Note: All AI service data has been preserved." 