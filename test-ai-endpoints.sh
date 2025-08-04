#!/bin/bash

echo "🔍 Testing AI Endpoints..."
echo "=========================="

# Wait for API to be ready
echo "⏳ Waiting for API to be ready..."
sleep 5

# Test 1: Basic API health
echo "📡 Testing API health..."
curl -s http://localhost:5001/health/ready
echo -e "\n"

# Test 2: AI Status
echo "🤖 Testing AI Status..."
curl -s http://localhost:5001/api/ai/status | jq .
echo -e "\n"

# Test 3: AI Test endpoint
echo "🧪 Testing AI Test endpoint..."
curl -s http://localhost:5001/api/ai/test | jq .
echo -e "\n"

# Test 4: Search suppliers (should return empty if no data)
echo "🔍 Testing supplier search..."
curl -s "http://localhost:5001/api/ai/search/suppliers?query=electronics&limit=5" | jq .
echo -e "\n"

# Test 5: Search items (should return empty if no data)
echo "🔍 Testing item search..."
curl -s "http://localhost:5001/api/ai/search/items?query=components&limit=5" | jq .
echo -e "\n"

echo "✅ AI endpoint testing completed!"
echo ""
echo "📋 Next steps:"
echo "1. If status shows 'unhealthy', check Ollama connectivity"
echo "2. If searches return empty results, you need to vectorize data first"
echo "3. Use POST /api/ai/vectorize/suppliers to vectorize supplier data"
echo "4. Use POST /api/ai/vectorize/items to vectorize item data" 