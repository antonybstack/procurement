# Grafana Observability Stack Setup

This document describes the Grafana observability stack setup for the procurement system.

## Overview

The Grafana observability stack provides comprehensive monitoring, logging, and tracing capabilities for the procurement system.

## Components

### Core Services

- **Grafana** (Port 3000): Dashboard and visualization platform
- **Prometheus** (Port 9090): Metrics collection and storage
- **Loki** (Port 3100): Log aggregation and querying
- **Tempo** (Port 3200): Distributed tracing backend

### Supporting Services

- **Promtail** (Port 9080): Log shipping agent
- **OTEL Collector** (Port 8888): OpenTelemetry data collection and routing

## Quick Start

### Start the Stack

```bash
# Start Grafana observability stack
./start-grafana.sh

# Or start all services including Grafana
./start.sh
```

### Check Status

```bash
# Check service status
./status.sh

# Check health
./health-check.sh
```

### Stop the Stack

```bash
# Stop all services including Grafana
./stop.sh
```

## Access Points

| Service | URL | Credentials | Description |
|---------|-----|-------------|-------------|
| Grafana | http://localhost:3000 | admin/admin | Main dashboard interface |
| Prometheus | http://localhost:9090 | - | Metrics query interface |
| Loki | http://localhost:3100 | - | Log query interface |
| Tempo | http://localhost:3200 | - | Trace query interface |
| Promtail | http://localhost:9080 | - | Log shipping status |
| OTEL Collector | http://localhost:8888 | - | Collector metrics |

## Configuration

### Grafana Configuration

Grafana is configured with:
- Admin password: `admin`
- Sign-up disabled for security
- TraceQL editor enabled
- Provisioned datasources and dashboards

### Data Sources

The following data sources are automatically configured:
- **Prometheus**: For metrics data
- **Loki**: For log data
- **Tempo**: For trace data

### Dashboards

Pre-configured dashboards include:
- System overview
- Application metrics
- Log analysis
- Distributed tracing

## Monitoring Your Application

### Metrics Collection

To collect metrics from your .NET API:

1. Add Prometheus metrics to your application
2. Configure Prometheus to scrape your API endpoints
3. View metrics in Grafana dashboards

### Log Collection

To collect logs:

1. Configure your application to output structured logs
2. Promtail will automatically collect container logs
3. View logs in Grafana's Explore section

### Distributed Tracing

To enable distributed tracing:

1. Add OpenTelemetry instrumentation to your application
2. Configure the OTEL Collector to receive traces
3. View traces in Grafana's Tempo section

## Troubleshooting

### Service Won't Start

```bash
# Check container status
docker-compose -f docker-compose.grafana.yml ps

# Check logs
docker-compose -f docker-compose.grafana.yml logs grafana
docker-compose -f docker-compose.grafana.yml logs prometheus
docker-compose -f docker-compose.grafana.yml logs loki
docker-compose -f docker-compose.grafana.yml logs tempo
```

### Health Check Issues

```bash
# Check individual service health
curl http://localhost:3000/api/health  # Grafana
curl http://localhost:9090/-/ready     # Prometheus
curl http://localhost:3100/ready       # Loki
curl http://localhost:3200/ready       # Tempo
```

### Port Conflicts

If you get port conflicts:

```bash
# Check what's using the ports
lsof -i :3000  # Grafana
lsof -i :9090  # Prometheus
lsof -i :3100  # Loki
lsof -i :3200  # Tempo
```

## Data Persistence

Data is stored in Docker volumes:
- `grafana-storage`: Grafana dashboards and configuration
- `prometheus-storage`: Prometheus metrics data
- `loki-storage`: Loki log data
- `tempo-storage`: Tempo trace data

## Backup and Restore

### Backup Volumes

```bash
# Backup Grafana data
docker run --rm -v grafana-storage:/data -v $(pwd):/backup alpine tar czf /backup/grafana_backup.tar.gz -C /data .

# Backup Prometheus data
docker run --rm -v prometheus-storage:/data -v $(pwd):/backup alpine tar czf /backup/prometheus_backup.tar.gz -C /data .
```

### Restore Volumes

```bash
# Restore Grafana data
docker run --rm -v grafana-storage:/data -v $(pwd):/backup alpine tar xzf /backup/grafana_backup.tar.gz -C /data
```

## Integration with Procurement API

The Grafana stack can be integrated with the procurement API for:

1. **Application Metrics**: Monitor API performance, response times, and error rates
2. **Business Metrics**: Track RFQ processing, supplier interactions, and quote submissions
3. **Infrastructure Monitoring**: Monitor database performance, container health, and resource usage
4. **Log Analysis**: Analyze application logs for debugging and operational insights
5. **Distributed Tracing**: Trace requests across the entire system for performance optimization

## Next Steps

1. Start the Grafana stack: `./start-grafana.sh`
2. Access Grafana at http://localhost:3000
3. Explore the pre-configured dashboards
4. Configure additional metrics collection for your application
5. Set up alerts for critical metrics 