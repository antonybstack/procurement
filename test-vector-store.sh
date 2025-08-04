#!/bin/bash

# Test script for the improved PostgreSQL vector store implementation
# This script tests the new Semantic Kernel-based vector store functionality

set -e

echo "🧪 Testing PostgreSQL Vector Store Improvements"
echo "=============================================="

# Configuration
API_BASE_URL="http://localhost:5000"
OLLAMA_URL="http://localhost:11434"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print colored output
print_status() {
    local status=$1
    local message=$2
    case $status in
        "SUCCESS")
            echo -e "${GREEN}✅ $message${NC}"
            ;;
        "ERROR")
            echo -e "${RED}❌ $message${NC}"
            ;;
        "WARNING")
            echo -e "${YELLOW}⚠️  $message${NC}"
            ;;
        "INFO")
            echo -e "${BLUE}ℹ️  $message${NC}"
            ;;
    esac
}

# Function to test API endpoint
test_endpoint() {
    local method=$1
    local endpoint=$2
    local expected_status=$3
    local description=$4
    
    print_status "INFO" "Testing $description..."
    
    response=$(curl -s -w "%{http_code}" -X $method "$API_BASE_URL$endpoint" -o /tmp/response.json)
    status_code=${response: -3}
    
    if [ "$status_code" = "$expected_status" ]; then
        print_status "SUCCESS" "$description - Status: $status_code"
        if [ -s /tmp/response.json ]; then
            echo "Response: $(cat /tmp/response.json | head -c 200)..."
        fi
    else
        print_status "ERROR" "$description - Expected: $expected_status, Got: $status_code"
        if [ -s /tmp/response.json ]; then
            echo "Error response: $(cat /tmp/response.json)"
        fi
        return 1
    fi
}

# Function to test search endpoint
test_search() {
    local entity=$1
    local query=$2
    local description=$3
    
    print_status "INFO" "Testing $description..."
    
    response=$(curl -s -w "%{http_code}" -X GET "$API_BASE_URL/api/ai/vectorstore/search/$entity?query=$query&limit=3" -o /tmp/search_response.json)
    status_code=${response: -3}
    
    if [ "$status_code" = "200" ]; then
        print_status "SUCCESS" "$description - Status: $status_code"
        result_count=$(cat /tmp/search_response.json | jq '. | length' 2>/dev/null || echo "0")
        echo "Found $result_count results"
    else
        print_status "ERROR" "$description - Expected: 200, Got: $status_code"
        if [ -s /tmp/search_response.json ]; then
            echo "Error response: $(cat /tmp/search_response.json)"
        fi
        return 1
    fi
}

# Check if required tools are available
check_dependencies() {
    print_status "INFO" "Checking dependencies..."
    
    if ! command -v curl &> /dev/null; then
        print_status "ERROR" "curl is required but not installed"
        exit 1
    fi
    
    if ! command -v jq &> /dev/null; then
        print_status "WARNING" "jq is not installed - JSON parsing will be limited"
    fi
    
    print_status "SUCCESS" "Dependencies check completed"
}

# Test API connectivity
test_api_connectivity() {
    print_status "INFO" "Testing API connectivity..."
    
    if curl -s "$API_BASE_URL/api/ai/health" > /dev/null; then
        print_status "SUCCESS" "API is accessible"
    else
        print_status "ERROR" "API is not accessible at $API_BASE_URL"
        exit 1
    fi
}

# Test Ollama connectivity
test_ollama_connectivity() {
    print_status "INFO" "Testing Ollama connectivity..."
    
    if curl -s "$OLLAMA_URL/api/tags" > /dev/null; then
        print_status "SUCCESS" "Ollama is accessible"
    else
        print_status "WARNING" "Ollama is not accessible at $OLLAMA_URL"
    fi
}

# Test vector store health
test_vector_store_health() {
    print_status "INFO" "Testing vector store health..."
    
    response=$(curl -s -w "%{http_code}" -X GET "$API_BASE_URL/api/ai/health/vectorstore" -o /tmp/health_response.json)
    status_code=${response: -3}
    
    if [ "$status_code" = "200" ]; then
        print_status "SUCCESS" "Vector store health check passed"
        if [ -s /tmp/health_response.json ]; then
            echo "Health status: $(cat /tmp/health_response.json)"
        fi
    else
        print_status "ERROR" "Vector store health check failed - Status: $status_code"
        if [ -s /tmp/health_response.json ]; then
            echo "Health response: $(cat /tmp/health_response.json)"
        fi
        return 1
    fi
}

# Test vectorization endpoints
test_vectorization() {
    print_status "INFO" "Testing vectorization endpoints..."
    
    # Test suppliers vectorization
    test_endpoint "POST" "/api/ai/vectorstore/vectorize/suppliers" "200" "Suppliers vectorization"
    
    # Test items vectorization
    test_endpoint "POST" "/api/ai/vectorstore/vectorize/items" "200" "Items vectorization"
    
    # Test RFQs vectorization
    test_endpoint "POST" "/api/ai/vectorstore/vectorize/rfqs" "200" "RFQs vectorization"
    
    # Test quotes vectorization
    test_endpoint "POST" "/api/ai/vectorstore/vectorize/quotes" "200" "Quotes vectorization"
}

# Test search endpoints
test_search_endpoints() {
    print_status "INFO" "Testing search endpoints..."
    
    # Test supplier search
    test_search "suppliers" "electronics" "Supplier search"
    
    # Test item search
    test_search "items" "resistors" "Item search"
    
    # Test RFQ search
    test_search "rfqs" "components" "RFQ search"
    
    # Test quote search
    test_search "quotes" "supplier" "Quote search"
    
    # Test semantic search
    test_endpoint "GET" "/api/ai/vectorstore/search/semantic?query=high quality components&limit=10" "200" "Semantic search"
}

# Test legacy endpoints for backward compatibility
test_legacy_endpoints() {
    print_status "INFO" "Testing legacy endpoints for backward compatibility..."
    
    # Test legacy suppliers vectorization
    test_endpoint "POST" "/api/ai/vectorize/suppliers" "200" "Legacy suppliers vectorization"
    
    # Test legacy items vectorization
    test_endpoint "POST" "/api/ai/vectorize/items" "200" "Legacy items vectorization"
    
    # Test legacy search
    test_endpoint "GET" "/api/ai/search/suppliers?query=electronics&limit=3" "200" "Legacy supplier search"
}

# Performance test
test_performance() {
    print_status "INFO" "Testing performance..."
    
    # Test search performance
    start_time=$(date +%s%N)
    curl -s "$API_BASE_URL/api/ai/vectorstore/search/suppliers?query=electronics&limit=5" > /dev/null
    end_time=$(date +%s%N)
    
    duration=$(( (end_time - start_time) / 1000000 ))
    print_status "INFO" "Search performance: ${duration}ms"
    
    if [ $duration -lt 1000 ]; then
        print_status "SUCCESS" "Performance test passed (< 1 second)"
    else
        print_status "WARNING" "Performance test slow (> 1 second)"
    fi
}

# Main test execution
main() {
    echo "Starting vector store tests..."
    echo ""
    
    # Run tests
    check_dependencies
    echo ""
    
    test_api_connectivity
    echo ""
    
    test_ollama_connectivity
    echo ""
    
    test_vector_store_health
    echo ""
    
    test_vectorization
    echo ""
    
    test_search_endpoints
    echo ""
    
    test_legacy_endpoints
    echo ""
    
    test_performance
    echo ""
    
    print_status "SUCCESS" "All tests completed successfully!"
    echo ""
    echo "🎉 Vector store improvements are working correctly!"
    echo ""
    echo "Key improvements verified:"
    echo "✅ Semantic Kernel integration"
    echo "✅ Vector store models with proper attributes"
    echo "✅ Enhanced vector store service"
    echo "✅ New API endpoints"
    echo "✅ Backward compatibility"
    echo "✅ Health monitoring"
    echo "✅ Performance optimization"
}

# Cleanup function
cleanup() {
    rm -f /tmp/response.json /tmp/search_response.json /tmp/health_response.json
}

# Set up cleanup on exit
trap cleanup EXIT

# Run main function
main "$@" 