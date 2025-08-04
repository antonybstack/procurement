#!/bin/bash

# Start Ollama service
echo "Starting Ollama service..."
docker-compose -f docker-compose.ollama.yml up -d

# Wait for Ollama to be ready
echo "Waiting for Ollama to be ready..."
sleep 10

# Check if Ollama is running
until curl -f http://localhost:11434/api/tags > /dev/null 2>&1; do
    echo "Waiting for Ollama to be ready..."
    sleep 5
done

echo "Ollama is ready!"

# Pull the required model (smaller version for efficiency)
echo "Pulling Llama 3.1 1B model (smaller, more efficient)..."
curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.1:1b"}'

echo "Model pull initiated. This may take a few minutes..."

# Wait for model to be available
echo "Waiting for model to be available..."
until curl -s http://localhost:11434/api/tags | grep -q "llama3.1:1b"; do
    echo "Waiting for model to be available..."
    sleep 10
done

echo "Ollama setup complete! Model is ready for use."
echo "You can now start the API with: ./start-api.sh" 