#!/bin/bash

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw observability; then
  echo "🔧 Creating external network: observability"
  docker network create observability
fi

# Start Grafana Observability Stack
echo "🚀 Starting Grafana Observability Stack..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Start the Grafana services
echo "📦 Starting Grafana, Prometheus, Loki, and Tempo..."
docker-compose -f docker-compose.grafana.yml up -d

# Check if services started successfully
if [ $? -eq 0 ]; then
    echo "✅ Grafana services started successfully!"
    echo "📊 Grafana: http://localhost:3000"
    echo "📈 Prometheus: http://localhost:9090"
    echo "📝 Loki: http://localhost:3100"
    echo "⏱️ Tempo: http://localhost:3200"
else
    echo "❌ Failed to start Grafana services"
    exit 1
fi

# Wait for Prometheus to be healthy
echo "⏳ Waiting for Prometheus to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check Prometheus status using docker exec
    if docker exec prometheus curl -s http://localhost:9090/-/ready > /dev/null 2>&1; then
        echo "✅ Prometheus is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Prometheus failed to become ready within $timeout seconds"
    echo "📋 Checking Prometheus logs..."
    docker-compose -f docker-compose.grafana.yml logs prometheus --tail=20
    exit 1
fi

# Wait for Loki to be healthy
echo "⏳ Waiting for Loki to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check Loki status using docker exec
    if docker exec loki curl -s http://localhost:3100/ready > /dev/null 2>&1; then
        echo "✅ Loki is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Loki failed to become ready within $timeout seconds"
    echo "📋 Checking Loki logs..."
    docker-compose -f docker-compose.grafana.yml logs loki --tail=20
    exit 1
fi

# Wait for Tempo to be healthy
echo "⏳ Waiting for Tempo to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check Tempo status using docker exec
    if docker exec tempo curl -s http://localhost:3200/ready > /dev/null 2>&1; then
        echo "✅ Tempo is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Tempo failed to become ready within $timeout seconds"
    echo "📋 Checking Tempo logs..."
    docker-compose -f docker-compose.grafana.yml logs tempo --tail=20
    exit 1
fi

# Wait for Grafana to be healthy
echo "⏳ Waiting for Grafana to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check Grafana status using docker exec
    if docker exec grafana curl -s http://localhost:3000/api/health > /dev/null 2>&1; then
        echo "✅ Grafana is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Grafana failed to become ready within $timeout seconds"
    echo "📋 Checking Grafana logs..."
    docker-compose -f docker-compose.grafana.yml logs grafana --tail=20
    exit 1
fi

echo "🎉 Grafana Observability Stack is ready!"
echo ""
echo "🔗 Access Points:"
echo "   📊 Grafana: http://localhost:3000 (admin/admin)"
echo "   📈 Prometheus: http://localhost:9090"
echo "   📝 Loki: http://localhost:3100"
echo "   ⏱️ Tempo: http://localhost:3200"
echo "   📋 Promtail: http://localhost:9080"
echo "   🔄 OTEL Collector: http://localhost:8888" 