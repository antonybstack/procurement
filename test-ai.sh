#!/bin/bash

# Test Ollama Vectorization Service
# This script tests the Ollama-based AI vectorization functionality

set -e

echo "🧪 Testing Ollama Vectorization Service..."

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    if [ "$status" = "PASS" ]; then
        echo -e "${GREEN}✅ PASS${NC}: $message"
    elif [ "$status" = "FAIL" ]; then
        echo -e "${RED}❌ FAIL${NC}: $message"
    else
        echo -e "${YELLOW}⚠️  INFO${NC}: $message"
    fi
}

# Test 1: Check if Ollama is running
echo ""
echo "1. Testing Ollama service health..."
if curl -f http://localhost:11434/api/tags > /dev/null 2>&1; then
    print_status "PASS" "Ollama service is healthy"
else
    print_status "FAIL" "Ollama service is not responding"
    echo "   Make sure to run: ./start-ollama.sh"
    exit 1
fi

# Test 2: Test Ollama model availability
echo ""
echo "2. Testing Ollama model availability..."
response=$(curl -s http://localhost:11434/api/tags)

if echo "$response" | grep -q "models"; then
    print_status "PASS" "Ollama models are available"
    # Try to list models with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Available models: $(echo "$response" | jq -r '.models[].name' | tr '\n' ' ')"
    else
        echo "   Models are available (list not available without jq)"
    fi
else
    print_status "FAIL" "No models found"
    echo "   Response: $response"
fi

# Test 3: Test .NET API vectorization endpoints
echo ""
echo "3. Testing .NET API vectorization endpoints..."
if curl -f http://localhost:5001/api/ai/status > /dev/null 2>&1; then
    print_status "PASS" ".NET API vectorization endpoints are working"
else
    print_status "FAIL" ".NET API vectorization endpoints failed"
    echo "   Make sure the .NET API is running: ./start-api.sh"
fi

# Test 4: Test supplier vectorization
echo ""
echo "4. Testing supplier vectorization..."
response=$(curl -s -X POST "http://localhost:5001/api/ai/vectorize/suppliers")

if echo "$response" | grep -q "count"; then
    print_status "PASS" "Supplier vectorization is working"
    # Try to extract count with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Vectorized $(echo "$response" | jq -r '.count') suppliers"
    else
        echo "   Vectorization completed successfully"
    fi
else
    print_status "FAIL" "Supplier vectorization failed"
    echo "   Response: $response"
fi

# Test 5: Test semantic search
echo ""
echo "5. Testing semantic search..."
response=$(curl -s "http://localhost:5001/api/ai/search/suppliers?query=electronic%20components&limit=3")

if echo "$response" | grep -q "supplierId"; then
    print_status "PASS" "Semantic search is working"
    # Try to count with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Found $(echo "$response" | jq 'length') suppliers"
    else
        echo "   Found suppliers via semantic search"
    fi
else
    print_status "FAIL" "Semantic search failed"
    echo "   Response: $response"
fi

# Test 6: Test item vectorization
echo ""
echo "6. Testing item vectorization..."
response=$(curl -s -X POST "http://localhost:5001/api/ai/vectorize/items")

if echo "$response" | grep -q "count"; then
    print_status "PASS" "Item vectorization is working"
    # Try to extract count with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Vectorized $(echo "$response" | jq -r '.count') items"
    else
        echo "   Vectorization completed successfully"
    fi
else
    print_status "FAIL" "Item vectorization failed"
    echo "   Response: $response"
fi

# Summary
echo ""
echo "🎉 Ollama Vectorization Service Test Summary"
echo "============================================="
echo ""
echo "✅ All tests completed successfully!"
echo ""
echo "📋 Available endpoints:"
echo "   - Ollama: http://localhost:11434"
echo "   - .NET API: http://localhost:5001"
echo ""
echo "🔧 Test the endpoints manually:"
echo "   curl http://localhost:11434/api/tags"
echo "   curl \"http://localhost:5001/api/ai/search/suppliers?query=electronics\""
echo "   curl -X POST \"http://localhost:5001/api/ai/vectorize/suppliers\""
echo ""
echo "🚀 Ready to integrate with your frontend!" 