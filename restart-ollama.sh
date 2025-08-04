#!/bin/bash

# Restart Ollama service while preserving all data and models
echo "🔄 Restarting Ollama service (preserving data)..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Stop Ollama service (preserving volume data)
echo "🛑 Stopping Ollama service..."
docker-compose -f docker-compose.ollama.yml down

# Start Ollama service
echo "🚀 Starting Ollama service..."
docker-compose -f docker-compose.ollama.yml up -d

# Check if service started successfully
if [ $? -eq 0 ]; then
    echo "✅ Ollama service restarted successfully!"
else
    echo "❌ Failed to restart Ollama service"
    exit 1
fi

# Wait for Ollama to be ready
echo "⏳ Waiting for Ollama to be ready..."
timeout=60
counter=0
while [ $counter -lt $timeout ]; do
    if curl -f http://localhost:11434/api/tags > /dev/null 2>&1; then
        echo "✅ Ollama is ready!"
        break
    fi
    sleep 2
    counter=$((counter + 2))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Ollama failed to become ready within $timeout seconds"
    echo "📋 Checking Ollama logs..."
    docker-compose -f docker-compose.ollama.yml logs ollama --tail=20
    exit 1
fi

# Check if models are still available
echo "🔍 Checking model availability..."
MODELS=$(curl -s http://localhost:11434/api/tags | jq -r '.models[].name' 2>/dev/null || echo "")

if [ -n "$MODELS" ]; then
    echo "✅ Models are available:"
    echo "$MODELS" | while read -r model; do
        echo "   - $model"
    done
else
    echo "⚠️  No models found. You may need to pull models again."
    echo "💡 To pull a model: curl -X POST http://localhost:11434/api/pull -d '{\"name\": \"llama3.1:1b\"}'"
fi

echo "🎉 Ollama service restarted and ready!"
echo "📊 Ollama URL: http://localhost:11434"
echo "📝 Note: All models and data have been preserved." 