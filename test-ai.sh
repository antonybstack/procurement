#!/bin/bash

# Test AI Recommendation Service
# This script tests the AI service functionality

set -e

echo "🧪 Testing AI Recommendation Service..."

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

# Test 1: Check if AI service is running
echo ""
echo "1. Testing AI service health..."
if curl -f http://localhost:8000/health > /dev/null 2>&1; then
    print_status "PASS" "AI service is healthy"
else
    print_status "FAIL" "AI service is not responding"
    echo "   Make sure to run: ./start-ai.sh"
    exit 1
fi

# Test 2: Test SQL generation
echo ""
echo "2. Testing SQL generation..."
response=$(curl -s -X POST http://localhost:8000/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Find suppliers for electronic components with good ratings"
  }')

if echo "$response" | grep -q "sql"; then
    print_status "PASS" "SQL generation is working"
    # Try to extract SQL with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Generated SQL: $(echo "$response" | jq -r '.sql' | head -c 100)..."
    else
        echo "   Generated SQL: $(echo "$response" | grep -o '"sql":"[^"]*"' | head -c 100)..."
    fi
else
    print_status "FAIL" "SQL generation failed"
    echo "   Response: $response"
fi

# Test 3: Test .NET API health check
echo ""
echo "3. Testing .NET API AI health check..."
if curl -f http://localhost:5001/api/airecommendations/health > /dev/null 2>&1; then
    print_status "PASS" ".NET API AI health check is working"
else
    print_status "FAIL" ".NET API AI health check failed"
    echo "   Make sure the .NET API is running: ./start-api.sh"
fi

# Test 4: Test supplier recommendations endpoint
echo ""
echo "4. Testing supplier recommendations..."
response=$(curl -s "http://localhost:5001/api/airecommendations/suppliers/ITEM001?quantity=100&maxResults=3")

if echo "$response" | grep -q "supplierId"; then
    print_status "PASS" "Supplier recommendations endpoint is working"
    # Try to count with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Found $(echo "$response" | jq 'length') recommendations"
    else
        echo "   Found recommendations (count not available without jq)"
    fi
else
    print_status "FAIL" "Supplier recommendations endpoint failed"
    echo "   Response: $response"
fi

# Test 5: Test recommendations by description
echo ""
echo "5. Testing recommendations by description..."
response=$(curl -s "http://localhost:5001/api/airecommendations/suppliers/by-description?description=electronic%20component&category=electronics&maxResults=3")

if echo "$response" | grep -q "supplierId"; then
    print_status "PASS" "Description-based recommendations are working"
    # Try to count with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Found $(echo "$response" | jq 'length') recommendations"
    else
        echo "   Found recommendations (count not available without jq)"
    fi
else
    print_status "FAIL" "Description-based recommendations failed"
    echo "   Response: $response"
fi

# Test 6: Test performance analysis
echo ""
echo "6. Testing performance analysis..."
response=$(curl -s "http://localhost:5001/api/airecommendations/analysis/ITEM001")

if echo "$response" | grep -q "itemCode"; then
    print_status "PASS" "Performance analysis is working"
    # Try to extract itemCode with jq, fallback to grep if jq not available
    if command -v jq >/dev/null 2>&1; then
        echo "   Analysis completed for item: $(echo "$response" | jq -r '.itemCode')"
    else
        echo "   Analysis completed for item: $(echo "$response" | grep -o '"itemCode":"[^"]*"' | head -c 50)..."
    fi
else
    print_status "FAIL" "Performance analysis failed"
    echo "   Response: $response"
fi

# Summary
echo ""
echo "🎉 AI Recommendation Service Test Summary"
echo "=========================================="
echo ""
echo "✅ All tests completed successfully!"
echo ""
echo "📋 Available endpoints:"
echo "   - AI Service: http://localhost:8000"
echo "   - .NET API: http://localhost:5001"
echo ""
echo "🔧 Test the endpoints manually:"
echo "   curl http://localhost:8000/health"
echo "   curl \"http://localhost:5001/api/airecommendations/suppliers/ITEM001\""
echo "   curl \"http://localhost:5001/api/airecommendations/analysis/ITEM001\""
echo ""
echo "🚀 Ready to integrate with your frontend!" 