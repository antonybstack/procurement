#!/bin/bash

# Start AI Recommendation Service
# This script starts the AI service for supplier recommendations

set -e

echo "🚀 Starting AI Recommendation Service..."

# Check if we're in the right directory
if [ ! -f "docker-compose.ai.mini.yml" ]; then
    echo "❌ Error: docker-compose.ai.mini.yml not found. Please run this script from the project root."
    exit 1
fi

# Check if postgres service is running
if ! docker ps | grep -q "postgres_db"; then
    echo "📡 Starting postgres service..."
    ./start-db.sh
fi

# Check if the postgres network exists
if ! docker network ls | grep -q "postgres_network"; then
    echo "📡 Creating postgres_network..."
    docker network create postgres_network
fi

# Start the AI service
echo "🤖 Starting AI Recommendation Service..."
docker-compose -f docker-compose.ai.mini.yml up -d

# Wait for the service to be healthy
echo "⏳ Waiting for AI service to be ready..."
for i in {1..30}; do
    if curl -f http://localhost:8000/health > /dev/null 2>&1; then
        echo "✅ AI Recommendation Service is ready!"
        echo "🌐 Service URL: http://localhost:8000"
        echo "📊 Health Check: http://localhost:8000/health"
        echo "🔧 API Endpoints:"
        echo "   - GET /health - Service health"
        echo "   - POST /generate - Generate SQL queries"
        echo "   - GET /schema - Get database schema"
        echo "   - GET /models - List available models"
        break
    fi
    echo "⏳ Waiting... (attempt $i/30)"
    sleep 2
done

if [ $i -eq 30 ]; then
    echo "❌ AI service failed to start within 60 seconds"
    echo "📋 Checking logs..."
    docker-compose -f docker-compose.ai.mini.yml logs ai-recommendation-service
    exit 1
fi

echo ""
echo "🎉 AI Recommendation Service started successfully!"
echo ""
echo "📝 Next steps:"
echo "1. Test the service: curl http://localhost:8000/health"
echo "2. The .NET API will automatically connect to this service"
echo "3. Use the AI recommendations in your procurement app"
echo ""
echo "🛑 To stop: docker-compose -f docker-compose.ai.mini.yml down" 