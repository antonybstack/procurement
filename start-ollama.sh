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

# Pull the required model (Llama 3.1 8B Instruct with tool/function calling)
# Using q4_K_M quantization which runs well on Apple Silicon
MODEL_TAG="llama3.1:8b-instruct-q4_K_M"
echo "Pulling ${MODEL_TAG} (optimized quantization for Apple Silicon) ..."
curl -X POST http://localhost:11434/api/pull -d '{"name": "'"${MODEL_TAG}"'"}'

echo "Model pull initiated. This may take a few minutes..."

# Wait for model to be available
echo "Waiting for model to be available..."
until curl -s http://localhost:11434/api/tags | grep -q "${MODEL_TAG}"; do
    echo "Waiting for model to be available..."
    sleep 10
done

echo "Ollama setup complete! Model is ready for use."
echo "You can now start the API with: ./start-api.sh" 