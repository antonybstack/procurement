#!/bin/bash

# Stop Angular Frontend service
echo "Stopping Angular Frontend service..."

# Stop the frontend service
docker-compose -f docker-compose.frontend.yml down

echo "✅ Angular Frontend service stopped" 