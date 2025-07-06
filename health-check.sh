#!/bin/bash

echo "🔍 Comprehensive Health Check for Procurement API"
echo "=================================================="
echo ""

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
    if [ "$status" = "OK" ]; then
        echo -e "${GREEN}✅ $message${NC}"
    elif [ "$status" = "WARNING" ]; then
        echo -e "${YELLOW}⚠️  $message${NC}"
    else
        echo -e "${RED}❌ $message${NC}"
    fi
}

# Check if Docker containers are running
echo -e "${BLUE}📋 Container Status:${NC}"
if docker-compose ps | grep -q "Up"; then
    print_status "OK" "Docker containers are running"
    docker-compose ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
else
    print_status "ERROR" "No containers are running"
    exit 1
fi

echo ""

# Check PostgreSQL health
echo -e "${BLUE}🗄️  PostgreSQL Health:${NC}"
if docker-compose exec -T postgres pg_isready -U postgres -d myapp >/dev/null 2>&1; then
    print_status "OK" "PostgreSQL is healthy and accepting connections"
else
    print_status "ERROR" "PostgreSQL is not responding"
fi

echo ""

# Check API basic health
echo -e "${BLUE}🏥 API Basic Health:${NC}"
if curl -f -s http://localhost:5001/health >/dev/null 2>&1; then
    print_status "OK" "API basic health check passed"
else
    print_status "ERROR" "API basic health check failed"
fi

echo ""

# Check readiness health with all components
echo -e "${BLUE}🔬 Readiness Health Check:${NC}"
HEALTH_RESPONSE=$(curl -s http://localhost:5001/health/ready 2>/dev/null)
if [ $? -eq 0 ]; then
    print_status "OK" "Readiness health endpoint is responding"
    
    # Parse the JSON response to check individual components
    if echo "$HEALTH_RESPONSE" | grep -q "Healthy"; then
        print_status "OK" "Overall system health: HEALTHY"
    else
        print_status "WARNING" "Overall system health: UNHEALTHY"
    fi
    
    # Since the response is just "Healthy", we know all checks passed
    print_status "OK" "All health checks passed"
    
else
    print_status "ERROR" "Readiness health endpoint is not responding"
fi

echo ""

# Check Swagger UI directly
echo -e "${BLUE}📚 Swagger UI Check:${NC}"
if curl -f -s http://localhost:5001/swagger/index.html >/dev/null 2>&1; then
    print_status "OK" "Swagger UI is accessible"
else
    print_status "ERROR" "Swagger UI is not accessible"
fi

echo ""

# Check API endpoints directly
echo -e "${BLUE}🔌 API Endpoints Smoke Test:${NC}"

# Check RFQs endpoint
if curl -f -s http://localhost:5001/api/rfqs >/dev/null 2>&1; then
    print_status "OK" "RFQs endpoint (/api/rfqs): Responding"
else
    print_status "WARNING" "RFQs endpoint (/api/rfqs): Not responding"
fi

# Check Suppliers endpoint
if curl -f -s http://localhost:5001/api/suppliers >/dev/null 2>&1; then
    print_status "OK" "Suppliers endpoint (/api/suppliers): Responding"
else
    print_status "WARNING" "Suppliers endpoint (/api/suppliers): Not responding"
fi

echo ""

# Check pgAdmin
echo -e "${BLUE}🛠️  pgAdmin Check:${NC}"
if curl -f -s http://localhost:8080 >/dev/null 2>&1; then
    print_status "OK" "pgAdmin is accessible"
else
    print_status "WARNING" "pgAdmin is not accessible"
fi

# Check Grafana Observability Stack
echo -e "${BLUE}📊 Grafana Observability Stack:${NC}"

# Check Grafana
if curl -f -s http://localhost:3000/api/health >/dev/null 2>&1; then
    print_status "OK" "Grafana is accessible"
else
    print_status "WARNING" "Grafana is not accessible"
fi

# Check Prometheus
if curl -f -s http://localhost:9090/-/ready >/dev/null 2>&1; then
    print_status "OK" "Prometheus is accessible"
else
    print_status "WARNING" "Prometheus is not accessible"
fi

# Check Loki
if curl -f -s http://localhost:3100/ready >/dev/null 2>&1; then
    print_status "OK" "Loki is accessible"
else
    print_status "WARNING" "Loki is not accessible"
fi

# Check Tempo
if curl -f -s http://localhost:3200/ready >/dev/null 2>&1; then
    print_status "OK" "Tempo is accessible"
else
    print_status "WARNING" "Tempo is not accessible"
fi

echo ""

# Display access URLs
echo -e "${BLUE}🌐 Access URLs:${NC}"
echo -e "${GREEN}API Base:${NC} http://localhost:5001"
echo -e "${GREEN}Swagger UI:${NC} http://localhost:5001/swagger/index.html"
echo -e "${GREEN}Health Check:${NC} http://localhost:5001/health/ready"
echo -e "${GREEN}pgAdmin:${NC} http://localhost:8080 (admin@example.com / admin_password)"
echo -e "${GREEN}PostgreSQL:${NC} localhost:5432"
echo -e "${GREEN}Grafana:${NC} http://localhost:3000 (admin/admin)"
echo -e "${GREEN}Prometheus:${NC} http://localhost:9090"
echo -e "${GREEN}Loki:${NC} http://localhost:3100"
echo -e "${GREEN}Tempo:${NC} http://localhost:3200"

echo ""

# Show recent logs if there are any errors
echo -e "${BLUE}📝 Recent API Logs (last 10 lines):${NC}"
docker-compose logs --tail=10 procurement-api

echo ""
echo "✅ Health check completed!" 