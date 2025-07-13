#!/bin/bash

# Main startup script for the procurement system
echo "🚀 Starting Procurement System..."
echo "=================================="

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Check if health-check.sh exists
if [ ! -f "./health-check.sh" ]; then
    echo "❌ health-check.sh not found"
    exit 1
fi

# Step 1: Start database services
echo ""
echo "📊 Step 1: Starting database services..."
if ! ./start-db.sh; then
    echo "❌ Failed to start database services"
    exit 1
fi

# Step 2: Start Elastic stack
echo ""
echo "🔍 Step 2: Starting Elastic stack..."
if ! ./start-elastic.sh; then
    echo "❌ Failed to start Elastic stack"
    exit 1
fi

# Step 3: Start API service
echo ""
echo "🌐 Step 3: Starting API service..."
if ! ./start-api.sh; then
    echo "❌ Failed to start API service"
    exit 1
fi

# Step 4: Start Frontend service
echo ""
echo "🎨 Step 4: Starting Frontend service..."
if ! ./start-frontend.sh; then
    echo "❌ Failed to start Frontend service"
    exit 1
fi

# Step 5: Start Grafana Observability Stack (optional)
echo ""
echo "📊 Step 4: Starting Grafana Observability Stack..."
if [ -f "./start-grafana.sh" ]; then
    if ! ./start-grafana.sh; then
        echo "⚠️  Failed to start Grafana services (continuing anyway)"
    fi
else
    echo "⚠️  start-grafana.sh not found (skipping Grafana)"
fi

# Step 6: Run health checks
echo ""
echo "🔍 Step 6: Running health checks..."
if ! ./health-check.sh; then
    echo "❌ Health checks failed"
    exit 1
fi

echo ""
echo "🎉 All services are up and running!"
echo "=================================="
echo "📊 PostgreSQL: localhost:5432"
echo "🗄️  pgAdmin: http://localhost:8080"
echo "   - Email: admin@example.com"
echo "   - Password: admin_password"
echo "🔍 Elasticsearch: http://localhost:9200"
echo "📊 Kibana: http://localhost:5601"
echo "🌐 API: http://localhost:5001"
echo "📚 Swagger: http://localhost:5001/swagger"
echo "🎨 Frontend: http://localhost:4200"
echo "📊 Grafana: http://localhost:3000 (admin/admin)"
echo "📈 Prometheus: http://localhost:9090"
echo "📝 Loki: http://localhost:3100"
echo "⏱️ Tempo: http://localhost:3200"
echo ""
echo "📝 Useful commands:"
echo "   - View logs: docker-compose -f docker-compose.db.yml logs -f"
echo "   - API logs: docker-compose -f docker-compose.api.yml logs -f"
echo "   - Frontend logs: docker-compose -f docker-compose.frontend.yml logs -f"
echo "   - Elastic logs: docker-compose -f docker-compose.elastic.yml logs -f"
echo "   - Grafana logs: docker-compose -f docker-compose.grafana.yml logs -f"
echo "   - Stop all: ./stop.sh"
echo "   - Restart API only: ./restart-api.sh" 