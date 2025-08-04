#!/bin/bash

# Reset Ollama service by removing all data and models
# WARNING: This will delete ALL models and data in Ollama!
# Use this only when you need a completely fresh Ollama installation.

echo "⚠️  WARNING: This will delete ALL Ollama models and data!"
echo "This action cannot be undone."
echo ""
read -p "Are you sure you want to reset Ollama? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Ollama reset cancelled."
    exit 0
fi

echo "🧹 Resetting Ollama (removing all models and data)..."

# Stop and remove Ollama containers AND volumes
echo "🛑 Stopping containers and removing data volumes..."
docker-compose -f docker-compose.ollama.yml down -v

# Remove the Ollama volume manually to ensure complete cleanup
echo "🗑️  Removing Ollama data volume..."
docker volume rm ollama_data 2>/dev/null || true

# Start Ollama service with fresh volume
echo "🚀 Starting fresh Ollama service..."
docker-compose -f docker-compose.ollama.yml up -d

# Check if service started successfully
if [ $? -eq 0 ]; then
    echo "✅ Ollama service started successfully!"
else
    echo "❌ Failed to start Ollama service"
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
    exit 1
fi

echo "🎉 Ollama has been reset and is ready!"
echo "📝 Note: All previous models have been removed."
echo ""
echo "💡 To pull models again, run:"
echo "   ./start-ollama.sh" 