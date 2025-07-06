#!/bin/bash

# Ensure the external network exists
if ! docker network ls --format '{{.Name}}' | grep -qw elastic_network; then
  echo "🔧 Creating external network: elastic_network"
  docker network create elastic_network
fi

# Start Elastic Stack (Elasticsearch + Kibana)
echo "🚀 Starting Elastic stack..."

# Check if docker-compose is available
if ! command -v docker-compose &> /dev/null; then
    echo "❌ docker-compose is not installed or not in PATH"
    exit 1
fi

# Start the Elastic services
echo "📦 Starting Elasticsearch and Kibana..."
docker-compose -f docker-compose.elastic.yml up -d

# Check if services started successfully
if [ $? -eq 0 ]; then
    echo "✅ Elastic services started successfully!"
    echo "🔍 Elasticsearch: http://localhost:9200 (es01 node)"
    echo "📊 Kibana: http://localhost:5601"
else
    echo "❌ Failed to start Elastic services"
    exit 1
fi

# Wait for Elasticsearch cluster to be healthy
echo "⏳ Waiting for Elasticsearch cluster to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check cluster health using docker exec on es01 (master node)
    if docker exec procurement-es01-1 curl -s --cacert /usr/share/elasticsearch/config/certs/ca/ca.crt -u "elastic:elastic_password" https://localhost:9200/_cluster/health > /dev/null 2>&1; then
        echo "✅ Elasticsearch cluster is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Elasticsearch cluster failed to become ready within $timeout seconds"
    echo "📋 Checking Elasticsearch logs..."
    docker-compose -f docker-compose.elastic.yml logs es01 --tail=20
    exit 1
fi

# Wait for Kibana to be healthy
echo "⏳ Waiting for Kibana to be ready..."
timeout=90
counter=0
while [ $counter -lt $timeout ]; do
    # Check Kibana status using docker exec
    if docker exec procurement-kibana-1 curl -s -I http://localhost:5601 | grep -q 'HTTP/1.1 302 Found'; then
        echo "✅ Kibana is ready!"
        break
    fi
    sleep 3
    counter=$((counter + 3))
    echo -n "."
done

if [ $counter -ge $timeout ]; then
    echo "❌ Kibana failed to become ready within $timeout seconds"
    echo "📋 Checking Kibana logs..."
    docker-compose -f docker-compose.elastic.yml logs kibana --tail=20
    exit 1
fi

echo "🎉 Elastic stack is ready!" 