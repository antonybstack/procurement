#!/bin/bash

# Setup Grafana Observability Stack
# This script creates the directory structure and configuration files

set -e

echo "🚀 Setting up Grafana Observability Stack..."

# Create directory structure
echo "📁 Creating directory structure..."
mkdir -p grafana/provisioning/datasources
mkdir -p grafana/provisioning/dashboards
mkdir -p tempo
mkdir -p loki
mkdir -p prometheus
mkdir -p promtail
mkdir -p otel-collector

echo "✅ Directory structure created"

# Create docker-compose.grafana.yml
echo "🐳 Creating docker-compose.grafana.yml..."
cat > docker-compose.grafana.yml << 'EOF'
version: '3.8'

services:
  # Grafana - Main Dashboard
  grafana:
    image: grafana/grafana:latest
    container_name: grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
      - GF_USERS_ALLOW_SIGN_UP=false
      - GF_FEATURE_TOGGLES_ENABLE=traceqlEditor
    volumes:
      - grafana-storage:/var/lib/grafana
      - ./grafana/provisioning:/etc/grafana/provisioning
    networks:
      - observability
    depends_on:
      - tempo
      - loki
      - prometheus

  # Tempo - Distributed Tracing
  tempo:
    image: grafana/tempo:latest
    container_name: tempo
    ports:
      - "3200:3200"   # Tempo HTTP
      - "4317:4317"   # OTLP gRPC
      - "4318:4318"   # OTLP HTTP
    volumes:
      - ./tempo/tempo.yaml:/etc/tempo.yaml
      - tempo-storage:/var/tempo
    command: [ "-config.file=/etc/tempo.yaml" ]
    networks:
      - observability

  # Loki - Log Aggregation  
  loki:
    image: grafana/loki:latest
    container_name: loki
    ports:
      - "3100:3100"
    volumes:
      - ./loki/loki.yaml:/etc/loki/local-config.yaml
      - loki-storage:/loki
    command: -config.file=/etc/loki/local-config.yaml
    networks:
      - observability

  # Prometheus - Metrics Collection
  prometheus:
    image: prom/prometheus:latest
    container_name: prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus/prometheus.yml:/etc/prometheus/prometheus.yml
      - prometheus-storage:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--web.enable-lifecycle'
    networks:
      - observability

  # Promtail - Log Shipper (for local log collection)
  promtail:
    image: grafana/promtail:latest
    container_name: promtail
    ports:
      - "9080:9080"
    volumes:
      - ./promtail/promtail.yaml:/etc/promtail/config.yml
      - /var/log:/var/log:ro
      - /var/lib/docker/containers:/var/lib/docker/containers:ro
    command: -config.file=/etc/promtail/config.yml
    networks:
      - observability
    depends_on:
      - loki

  # OTEL Collector (Optional - for advanced routing/processing)
  otel-collector:
    image: otel/opentelemetry-collector-contrib:latest
    container_name: otel-collector
    ports:
      - "4319:4317"   # OTLP gRPC receiver (different port to avoid conflict)
      - "4320:4318"   # OTLP HTTP receiver (different port to avoid conflict)
      - "8888:8888"   # Prometheus metrics
    volumes:
      - ./otel-collector/otel-collector.yaml:/etc/otel-collector-config.yaml
    command: ["--config=/etc/otel-collector-config.yaml"]
    networks:
      - observability
    depends_on:
      - tempo
      - loki
      - prometheus

networks:
  observability:
    driver: bridge

volumes:
  grafana-storage:
  tempo-storage:
  loki-storage:
  prometheus-storage:
EOF

echo "✅ docker-compose.grafana.yml created"

# Create Tempo configuration
echo "⏱️ Creating Tempo configuration..."
cat > tempo/tempo.yaml << 'EOF'
server:
  http_listen_port: 3200

distributor:
  receivers:
    otlp:
      protocols:
        grpc:
          endpoint: 0.0.0.0:4317
        http:
          endpoint: 0.0.0.0:4318

ingester:
  max_block_duration: 5m

compactor:
  compaction:
    block_retention: 72h

metrics_generator:
  registry:
    external_labels:
      source: tempo
      cluster: docker-compose
  storage:
    path: /var/tempo/generator/wal
    remote_write:
      - url: http://prometheus:9090/api/v1/write
        send_exemplars: true

storage:
  trace:
    backend: local
    local:
      path: /var/tempo/traces
    wal:
      path: /var/tempo/wal
    pool:
      max_workers: 100
      queue_depth: 10000

query_frontend:
  search:
    duration_slo: 5s
    throughput_bytes_slo: 1.073741824e+09
  trace_by_id:
    duration_slo: 5s

overrides:
  defaults:
    metrics_generator:
      processors: [service-graphs, span-metrics]
EOF

echo "✅ Tempo configuration created"

# Create Loki configuration
echo "📝 Creating Loki configuration..."
cat > loki/loki.yaml << 'EOF'
auth_enabled: false

server:
  http_listen_port: 3100

common:
  instance_addr: 127.0.0.1
  path_prefix: /loki
  storage:
    filesystem:
      chunks_directory: /loki/chunks
      rules_directory: /loki/rules
  replication_factor: 1
  ring:
    kvstore:
      store: inmemory

query_range:
  results_cache:
    cache:
      embedded_cache:
        enabled: true
        max_size_mb: 100

schema_config:
  configs:
    - from: 2020-10-24
      store: boltdb-shipper
      object_store: filesystem
      schema: v11
      index:
        prefix: index_
        period: 24h

ruler:
  alertmanager_url: http://localhost:9093

analytics:
  reporting_enabled: false
EOF

echo "✅ Loki configuration created"

# Create Prometheus configuration
echo "📊 Creating Prometheus configuration..."
cat > prometheus/prometheus.yml << 'EOF'
global:
  scrape_interval: 15s
  evaluation_interval: 15s

rule_files:
  # - "first_rules.yml"
  # - "second_rules.yml"

scrape_configs:
  - job_name: 'prometheus'
    static_configs:
      - targets: ['localhost:9090']

  - job_name: 'tempo'
    static_configs:
      - targets: ['tempo:3200']

  - job_name: 'loki'
    static_configs:
      - targets: ['loki:3100']

  - job_name: 'grafana'
    static_configs:
      - targets: ['grafana:3000']

  # Add your .NET applications here
  - job_name: 'dotnet-app'
    static_configs:
      - targets: ['your-dotnet-app:5000']  # Replace with your app's host:port
    metrics_path: '/metrics'
    scrape_interval: 10s

  # OTEL Collector metrics
  - job_name: 'otel-collector'
    static_configs:
      - targets: ['otel-collector:8888']
EOF

echo "✅ Prometheus configuration created"

# Create Promtail configuration
echo "🚛 Creating Promtail configuration..."
cat > promtail/promtail.yaml << 'EOF'
server:
  http_listen_port: 9080
  grpc_listen_port: 0

positions:
  filename: /tmp/positions.yaml

clients:
  - url: http://loki:3100/loki/api/v1/push

scrape_configs:
  - job_name: containers
    static_configs:
      - targets:
          - localhost
        labels:
          job: containerlogs
          __path__: /var/lib/docker/containers/*/*log

    pipeline_stages:
      - json:
          expressions:
            output: log
            stream: stream
            attrs:
      - json:
          expressions:
            tag:
          source: attrs
      - regex:
          expression: (?P<container_name>(?:[^|]*))\|
          source: tag
      - timestamp:
          format: RFC3339Nano
          source: time
      - labels:
          stream:
          container_name:
      - output:
          source: output

  - job_name: system
    static_configs:
      - targets:
          - localhost
        labels:
          job: varlogs
          __path__: /var/log/*log
EOF

echo "✅ Promtail configuration created"

# Create Grafana datasources
echo "📈 Creating Grafana datasources configuration..."
cat > grafana/provisioning/datasources/datasources.yaml << 'EOF'
apiVersion: 1

datasources:
  # Tempo for Traces
  - name: Tempo
    type: tempo
    access: proxy
    url: http://tempo:3200
    uid: tempo
    isDefault: false
    jsonData:
      tracesToLogsV2:
        datasourceUid: 'loki'
        tags: [{ key: 'service.name', value: 'service' }]
      tracesToMetrics:
        datasourceUid: 'prometheus'
        tags: [{ key: 'service.name', value: 'service' }]
      serviceMap:
        datasourceUid: 'prometheus'
      nodeGraph:
        enabled: true
      search:
        hide: false
      lokiSearch:
        datasourceUid: 'loki'

  # Loki for Logs
  - name: Loki
    type: loki
    access: proxy
    url: http://loki:3100
    uid: loki
    isDefault: false
    jsonData:
      maxLines: 1000
      derivedFields:
        - datasourceUid: tempo
          matcherRegex: "trace_id=(\\w+)"
          name: TraceID
          url: "$${__value.raw}"

  # Prometheus for Metrics
  - name: Prometheus
    type: prometheus
    access: proxy
    url: http://prometheus:9090
    uid: prometheus
    isDefault: true
    jsonData:
      exemplars: true
      httpMethod: POST
      prometheusType: Prometheus
      prometheusVersion: 2.40.0
    version: 1
EOF

echo "✅ Grafana datasources configuration created"

# Create OTEL Collector configuration
echo "🔄 Creating OTEL Collector configuration..."
cat > otel-collector/otel-collector.yaml << 'EOF'
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
  memory_limiter:
    limit_mib: 512

exporters:
  otlp/tempo:
    endpoint: tempo:4317
    tls:
      insecure: true
  
  loki:
    endpoint: http://loki:3100/loki/api/v1/push
    
  prometheus:
    endpoint: "0.0.0.0:8888"

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [otlp/tempo]
    
    logs:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [loki]
    
    metrics:
      receivers: [otlp]
      processors: [memory_limiter, batch]
      exporters: [prometheus]
EOF

echo "✅ OTEL Collector configuration created"

# Create README
echo "📚 Creating README..."
cat > README.md << 'EOF'
# Grafana Observability Stack

## Quick Start

1. Start the stack:
```bash
docker-compose up -d
```

2. Access services:
- Grafana: http://localhost:3000 (admin/admin)
- Prometheus: http://localhost:9090
- Tempo: http://localhost:3200

## Configuration

### .NET Applications
Configure your .NET apps to send traces to:
- OTLP gRPC: `http://localhost:4317`
- OTLP HTTP: `http://localhost:4318`

### Prometheus Scraping
Update `prometheus/prometheus.yml` with your application endpoints.

### Logs
- Promtail automatically collects Docker container logs
- Configure your apps to include trace IDs in logs for correlation

## Stopping
```bash
docker-compose down
```

## Data Persistence
All data is stored in Docker volumes and persists between restarts.
EOF

echo "✅ README created"

# Make the script executable
chmod +x setup-grafana-stack.sh

echo ""
echo "🎉 Grafana Observability Stack setup complete!"
echo ""
echo "📂 Generated files:"
echo "   ├── docker-compose.grafana.yml"
echo "   ├── README.md"
echo "   ├── grafana/provisioning/datasources/datasources.yaml"
echo "   ├── tempo/tempo.yaml"
echo "   ├── loki/loki.yaml"
echo "   ├── prometheus/prometheus.yml"
echo "   ├── promtail/promtail.yaml"
echo "   └── otel-collector/otel-collector.yaml"
echo ""
echo "🚀 To start the stack:"
echo "   docker-compose up -d"
echo ""
echo "🌐 Access points:"
echo "   - Grafana: http://localhost:3000 (admin/admin)"
echo "   - Prometheus: http://localhost:9090"
echo "   - Tempo: http://localhost:3200"
echo ""
echo "⚙️  Configure your .NET apps to send traces to:"
echo "   - OTLP gRPC: http://localhost:4317"
echo "   - OTLP HTTP: http://localhost:4318"
echo ""
echo "📝 Don't forget to update prometheus/prometheus.yml with your app endpoints!"
