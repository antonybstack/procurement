#!/bin/bash

echo "Starting observability stack for local development..."

# Start the observability stack
docker-compose -f docker-compose.grafana.yml up -d

echo "Waiting for services to be ready..."
sleep 10

# Check if services are running
echo "Checking service status..."
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" | grep -E "(grafana|loki|tempo|otel-collector|prometheus)"

echo ""
echo "Observability stack is ready!"
echo "Grafana: http://localhost:3000 (admin/admin)"
echo "Loki: http://localhost:3100"
echo "Tempo: http://localhost:3200"
echo "Prometheus: http://localhost:9090"
echo "OTEL Collector: http://localhost:4319 (gRPC), http://localhost:4320 (HTTP)"
echo ""
echo "Your .NET API can now send logs to: http://localhost:4319" 