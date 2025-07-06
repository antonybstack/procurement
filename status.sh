#!/bin/bash

# Show status of all services
echo "📊 Service Status"
echo "================="

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

echo ""
echo "🗄️  Database Services:"
echo "---------------------"
docker-compose -f docker-compose.db.yml ps

echo ""
echo "🔍 Elastic Services:"
echo "-------------------"
docker-compose -f docker-compose.elastic.yml ps

echo ""
echo "🌐 API Services:"
echo "---------------"
docker-compose -f docker-compose.api.yml ps

echo ""
echo "📊 Grafana Services:"
echo "-------------------"
docker-compose -f docker-compose.grafana.yml ps

echo ""
echo "🔍 Health Checks:"
echo "----------------"

# Check PostgreSQL
echo -n "PostgreSQL: "
if docker-compose -f docker-compose.db.yml exec -T postgres pg_isready -U postgres -d myapp > /dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

# Check API
echo -n "API: "
if curl -f http://localhost:5001/health/ready > /dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

# Check pgAdmin
echo -n "pgAdmin: "
if curl -f http://localhost:8080 > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

# Check Elasticsearch
echo -n "Elasticsearch: "
if curl -f http://localhost:9200/_cluster/health > /dev/null 2>&1; then
    echo "✅ Healthy"
else
    echo "❌ Unhealthy"
fi

# Check Kibana
echo -n "Kibana: "
if curl -f http://localhost:5601/api/status > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

# Check Grafana
echo -n "Grafana: "
if curl -f http://localhost:3000/api/health > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

# Check Prometheus
echo -n "Prometheus: "
if curl -f http://localhost:9090/-/ready > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

# Check Loki
echo -n "Loki: "
if curl -f http://localhost:3100/ready > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

# Check Tempo
echo -n "Tempo: "
if curl -f http://localhost:3200/ready > /dev/null 2>&1; then
    echo "✅ Running"
else
    echo "❌ Not responding"
fi

echo ""
echo "🌐 Access Points:"
echo "----------------"
echo "📊 PostgreSQL: localhost:5432"
echo "🗄️  pgAdmin: http://localhost:8080"
echo "   - Email: admin@example.com"
echo "   - Password: admin_password"
echo "🔍 Elasticsearch: http://localhost:9200"
echo "📊 Kibana: http://localhost:5601"
echo "🌐 API: http://localhost:5001"
echo "📚 Swagger: http://localhost:5001/swagger"
echo "📊 Grafana: http://localhost:3000 (admin/admin)"
echo "📈 Prometheus: http://localhost:9090"
echo "📝 Loki: http://localhost:3100"
echo "⏱️ Tempo: http://localhost:3200" 