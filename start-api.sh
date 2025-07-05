#!/bin/bash

echo "🚀 Starting Procurement API with PostgreSQL..."
echo "📊 Services to be started:"
echo "   - PostgreSQL (port 5432)"
echo "   - ProcurementAPI (port 5001)"
echo "   - pgAdmin (port 8080)"
echo ""å

# Build and start all services
docker-compose up --build -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 10

# Check service status
echo ""
echo "📋 Service Status:"
docker-compose ps

echo ""
echo "🔍 Health Check Results:"
echo "PostgreSQL:"
docker-compose exec -T postgres pg_isready -U postgres -d myapp

echo ""
echo "ProcurementAPI:"
curl -f http://localhost:5001/health/ready 2>/dev/null || echo "API not ready yet"

echo ""
echo "✅ Services are starting up!"
echo "🌐 Access points:"
echo "   - API: http://localhost:5001"
echo "   - Swagger: http://localhost:5001/swagger"
echo "   - pgAdmin: http://localhost:8080 (admin@example.com / admin_password)"
echo ""
echo "📝 To view logs: docker-compose logs -f"
echo "🛑 To stop: docker-compose down" 