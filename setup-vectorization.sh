#!/bin/bash

echo "🚀 Setting up AI Vectorization Infrastructure for Procurement System"
echo "================================================================"

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
    exit 1
fi

# Check available disk space
echo "💾 Checking available disk space..."
AVAILABLE_SPACE=$(df -h . | awk 'NR==2 {print $4}' | sed 's/G//')
if [ "$AVAILABLE_SPACE" -lt 5 ]; then
    echo "❌ Low disk space detected. Please free up at least 5GB of space."
    echo "Available space: ${AVAILABLE_SPACE}GB"
    exit 1
fi
echo "✅ Sufficient disk space available: ${AVAILABLE_SPACE}GB"

# Step 1: Start database and apply vectorization schema
echo "📊 Step 1: Starting database and applying vectorization schema..."
./start-db.sh

# Wait for database to be ready
echo "⏳ Waiting for database to be ready..."
sleep 10

# Apply vectorization schema
echo "🔧 Applying vectorization schema to database..."
docker exec postgres_db psql -U postgres -d procurement -f /docker-entrypoint-initdb.d/database-schema.sql

# Step 2: Start Ollama
echo "🤖 Step 2: Starting Ollama LLM service..."
./start-ollama.sh

# Step 3: Build and start the API
echo "🔨 Step 3: Building and starting the API..."
./start-api.sh

# Step 4: Vectorize existing data
echo "📈 Step 4: Vectorizing existing data..."
echo "⏳ Waiting for API to be ready..."
sleep 30

# Vectorize suppliers
echo "🔄 Vectorizing suppliers..."
curl -X POST http://localhost:5000/api/ai/vectorize/suppliers

# Vectorize items
echo "🔄 Vectorizing items..."
curl -X POST http://localhost:5000/api/ai/vectorize/items

# Step 5: Test the system
echo "🧪 Step 5: Testing the AI system..."
echo "Testing vectorization service status..."
curl http://localhost:5000/api/ai/status

echo ""
echo "✅ Vectorization setup complete!"
echo ""
echo "🎯 Available AI endpoints:"
echo "  - POST /api/ai/vectorize/suppliers    - Vectorize all suppliers"
echo "  - POST /api/ai/vectorize/items        - Vectorize all items"
echo "  - GET  /api/ai/search/suppliers       - Search suppliers by query"
echo "  - GET  /api/ai/search/items           - Search items by query"
echo "  - GET  /api/ai/search/semantic        - Semantic search across all entities"
echo "  - GET  /api/ai/suggest/suppliers      - Suggest suppliers for requirements"
echo "  - GET  /api/ai/status                 - Check vectorization service status"
echo ""
echo "🔍 Example queries:"
echo "  - http://localhost:5000/api/ai/search/suppliers?query=aluminum+supplier"
echo "  - http://localhost:5000/api/ai/search/items?query=electronic+components"
echo "  - http://localhost:5000/api/ai/suggest/suppliers?requirement=AS9100+certified+supplier"
echo ""
echo "📚 Next steps:"
echo "  1. Test the AI endpoints with your data"
echo "  2. Integrate AI search into your Angular frontend"
echo "  3. Monitor performance and adjust vectorization as needed" 