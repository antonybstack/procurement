# Grafana Stack Troubleshooting Guide

This guide helps diagnose and fix issues with the Grafana observability stack, particularly the Loki connectivity problem.

## 🚨 Common Issues

### 1. Loki Connectivity Error
**Error**: `dial tcp: lookup loki on 127.0.0.11:53: no such host`

**Cause**: This error occurs when Grafana cannot resolve the `loki` hostname within the Docker network.

**Solutions**:

#### Solution A: Restart the Grafana Stack
```bash
# Stop all services
docker-compose -f docker-compose.grafana.yml down

# Remove orphaned containers
docker-compose -f docker-compose.grafana.yml down --remove-orphans

# Start services
docker-compose -f docker-compose.grafana.yml up -d

# Wait for services to be ready
sleep 15
```

#### Solution B: Use the Fix Script
```bash
# Run the diagnostics and fix script
./fix-grafana.sh

# Or just restart the stack
./fix-grafana.sh --restart
```

#### Solution C: Manual Network Check
```bash
# Check if services are on the same network
docker network ls | grep observability

# Check service IPs
docker inspect grafana --format "{{.NetworkSettings.Networks.observability.IPAddress}}"
docker inspect loki --format "{{.NetworkSettings.Networks.observability.IPAddress}}"

# Test connectivity from Grafana to Loki
docker exec grafana ping loki
docker exec grafana wget -q -O- http://loki:3100/ready
```

### 2. Services Not Starting

**Check service status**:
```bash
docker-compose -f docker-compose.grafana.yml ps
```

**Check service logs**:
```bash
docker logs grafana
docker logs loki
docker logs tempo
docker logs prometheus
docker logs promtail
docker logs otel-collector
```

### 3. Port Conflicts

**Check if ports are already in use**:
```bash
# Check ports 3000, 3100, 3200, 9090
netstat -tulpn | grep -E ':(3000|3100|3200|9090)'
```

**Fix**: Stop conflicting services or change ports in `docker-compose.grafana.yml`

## 🔧 Configuration Fixes

### 1. Loki Configuration Fix
The Loki configuration has been updated to fix the `instance_addr` issue:

**Before**:
```yaml
common:
  instance_addr: 127.0.0.1  # This was causing issues
```

**After**:
```yaml
common:
  instance_addr: 0.0.0.0    # Fixed - allows external connections
```

### 2. Grafana Datasource Configuration
Ensure the datasource configuration is correct in `grafana/provisioning/datasources/datasources.yaml`:

```yaml
- name: Loki
  type: loki
  access: proxy
  url: http://loki:3100  # This should work after restart
  uid: loki
```

## 🛠️ Diagnostic Tools

### 1. Use the Fix Script
```bash
# Full diagnostics
./fix-grafana.sh

# Check specific service logs
./fix-grafana.sh --logs loki
./fix-grafana.sh --logs grafana

# Show troubleshooting steps
./fix-grafana.sh --troubleshoot
```

### 2. Manual Health Checks
```bash
# Check if services are responding
curl http://localhost:3000/api/health  # Grafana
curl http://localhost:3100/ready       # Loki
curl http://localhost:3200/ready       # Tempo
curl http://localhost:9090/-/healthy   # Prometheus
```

### 3. Network Diagnostics
```bash
# Check Docker networks
docker network ls

# Inspect observability network
docker network inspect observability

# Check service connectivity
docker exec grafana nslookup loki
docker exec grafana wget -q -O- http://loki:3100/ready
```

## 📋 Step-by-Step Resolution

### Step 1: Stop All Services
```bash
docker-compose -f docker-compose.grafana.yml down
```

### Step 2: Clean Up
```bash
# Remove orphaned containers
docker-compose -f docker-compose.grafana.yml down --remove-orphans

# Remove any dangling networks
docker network prune -f
```

### Step 3: Verify Configuration
```bash
# Check if configuration files exist
ls -la grafana/provisioning/datasources/
ls -la loki/
ls -la promtail/
ls -la prometheus/
ls -la tempo/
```

### Step 4: Start Services
```bash
docker-compose -f docker-compose.grafana.yml up -d
```

### Step 5: Wait and Verify
```bash
# Wait for services to be ready
sleep 15

# Check service status
docker-compose -f docker-compose.grafana.yml ps

# Test connectivity
curl http://localhost:3100/ready  # Loki
curl http://localhost:3000/api/health  # Grafana
```

### Step 6: Test in Grafana
1. Open http://localhost:3000
2. Login with admin/admin
3. Go to Explore
4. Select Loki datasource
5. Try a simple query: `{}`

## 🔍 Advanced Troubleshooting

### 1. Check Service Dependencies
```bash
# Check if all required services are running
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

### 2. Verify Network Configuration
```bash
# Check if all services are on the same network
for service in grafana loki tempo prometheus promtail otel-collector; do
  echo "=== $service ==="
  docker inspect $service --format "{{.NetworkSettings.Networks.observability.IPAddress}}"
done
```

### 3. Check Resource Usage
```bash
# Check if services have enough resources
docker stats --no-stream
```

### 4. Verify Log Collection
```bash
# Check if Promtail is collecting logs
docker logs promtail --tail 20

# Check if logs are reaching Loki
curl -G -s "http://localhost:3100/loki/api/v1/labels" | jq .
```

## 🚀 Quick Fix Commands

### Complete Reset
```bash
# Stop everything
docker-compose -f docker-compose.grafana.yml down --remove-orphans

# Clean up
docker system prune -f

# Start fresh
docker-compose -f docker-compose.grafana.yml up -d

# Wait and test
sleep 20
curl http://localhost:3100/ready
curl http://localhost:3000/api/health
```

### Using the Fix Script
```bash
# Make script executable
chmod +x fix-grafana.sh

# Run full diagnostics and fix
./fix-grafana.sh

# Or just restart
./fix-grafana.sh --restart
```

## 📞 Getting Help

If the issue persists:

1. **Check logs**: `./fix-grafana.sh --logs grafana`
2. **Verify network**: `docker network inspect observability`
3. **Test connectivity**: `docker exec grafana ping loki`
4. **Check configuration**: Verify all YAML files are correct
5. **Restart Docker**: Sometimes Docker networking needs a restart

## 🎯 Expected Behavior

After fixing the issues:

- ✅ Grafana accessible at http://localhost:3000
- ✅ Loki responding at http://localhost:3100
- ✅ Logs visible in Grafana Explore with Loki datasource
- ✅ All services showing as "healthy" in `docker-compose ps`
- ✅ No DNS resolution errors in Grafana logs 