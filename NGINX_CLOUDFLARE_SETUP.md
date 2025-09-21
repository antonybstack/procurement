# Nginx + Cloudflare Tunnel Deployment Guide

This document describes the production deployment setup for the Procurement Management System using nginx reverse proxy with Cloudflare Tunnel for secure public access without port forwarding.

## Architecture Overview

The deployment uses a three-tier architecture:

```
Internet → Cloudflare CDN → Cloudflare Tunnel → Local nginx → Backend Services
```

### Components

1. **Cloudflare CDN & DNS**: Provides DNS, SSL termination, DDoS protection, and global CDN
2. **Cloudflare Tunnel**: Secure outbound-only connection from local machine to Cloudflare
3. **nginx Reverse Proxy**: High-performance reverse proxy with security headers and rate limiting
4. **Backend Services**: 
   - Angular frontend (port 4200)
   - .NET API (port 5001)
   - PostgreSQL database (port 5432)

## Why This Approach?

### Benefits
- **No Port Forwarding**: Avoids opening router ports and firewall holes
- **Enhanced Security**: Cloudflare provides DDoS protection and WAF
- **SSL/TLS**: Automatic SSL certificates and modern TLS protocols
- **Performance**: Global CDN and optimized nginx configuration
- **Professional**: Custom domain with production-grade setup
- **Zero Router Configuration**: Works behind any firewall/NAT

### vs. Alternatives
- **vs. Port Forwarding**: More secure, no router config needed
- **vs. Cloud VPS**: Lower cost, uses existing hardware
- **vs. ngrok**: Custom domain, no bandwidth limits, better performance

## Setup Process

### 1. Domain Configuration
- **Domain**: `sparkify.dev` (managed through Squarespace)
- **DNS**: Transferred to Cloudflare nameservers for tunnel support
- **SSL**: Cloudflare provides SSL termination + local nginx SSL

### 2. Local nginx Configuration
Location: `/opt/homebrew/etc/nginx/nginx.conf`

**Key Features**:
- **Auto-scaling**: `worker_processes auto` (detects CPU cores)
- **High Performance**: 8192 connections per worker, keepalive connections
- **Security Headers**: CSP, XSS protection, frame options (no HSTS at origin)
- **Rate Limiting**: 100 req/s for API, 200 req/s for frontend
- **HTTP/1.1 origin**: HTTP/2/3 handled by Cloudflare at the edge
- **Gzip Compression**: Optimized for web assets

**Routing**:
- `/` → Angular frontend (port 4200)
- `/api/*` → .NET API (port 5001)
- `/api/swagger/*` → API documentation

### 3. Cloudflare Tunnel Setup
- **Tunnel ID**: `392abfe9-a2db-41dd-a688-69e887823cdc`
- **Config**: `/Users/antbly/.cloudflared/config.yml`
- **Service**: Connects to `http://localhost:80` (local nginx)

Tip: Prefer `localhost` (or `127.0.0.1`) as the origin in the tunnel config to avoid breakage when your machine’s LAN IP changes (e.g., switching Wi‑Fi/Ethernet).

## Tools Used

### Development Tools
- **Homebrew**: Package manager for macOS dependencies
- **mkcert**: Local SSL certificate generation for development
- **nginx**: High-performance reverse proxy server
- **cloudflared**: Cloudflare Tunnel client

### Configuration Management
- **nginx.conf**: Production-optimized reverse proxy configuration
- **cloudflared config.yml**: Tunnel routing configuration
- **mac-deploy.sh**: Automated deployment script
- **/etc/hosts**: Local domain resolution

### Monitoring & Debugging
- **curl**: HTTP testing and performance measurement
- **dig/nslookup**: DNS resolution testing
- **nginx logs**: `/opt/homebrew/var/log/nginx/`
- **cloudflared logs**: Real-time tunnel connection status

## Current Configuration

### Domain & DNS
```bash
# DNS Resolution
dig +short sparkify.dev
# Returns: 104.21.15.53, 172.67.161.183 (Cloudflare IPs)

# Local hosts file
/etc/hosts: 127.0.0.1 sparkify.dev
```

### SSL Certificates
TLS terminates at Cloudflare. Local certificates are not required for the origin.

### Service Status
```bash
# Check nginx system service status
sudo launchctl list | grep nginx

# Check tunnel system service status
sudo launchctl list | grep cloudflare

# Test endpoints
curl -I https://sparkify.dev
curl https://sparkify.dev/api/health
```

## Maintenance & Operations

### Starting Services
```bash
# Start nginx system service
sudo launchctl start dev.sparkify.nginx

# Start Cloudflare tunnel system service
sudo launchctl start com.cloudflare.sparkify

# Manual start (if system services not configured)
cloudflared tunnel run sparkify
```

### Stopping Services
```bash
# Stop nginx system service
sudo launchctl stop dev.sparkify.nginx

# Stop tunnel system service
sudo launchctl stop com.cloudflare.sparkify

# Manual stop (if needed)
sudo pkill nginx
sudo pkill cloudflared
```

### Configuration Updates
```bash
# Validate nginx config
sudo nginx -t

# Restart nginx system service
sudo launchctl stop dev.sparkify.nginx
sudo launchctl start dev.sparkify.nginx

# Restart tunnel system service
sudo launchctl stop com.cloudflare.sparkify
sudo launchctl start com.cloudflare.sparkify
```

### Log Monitoring
```bash
# nginx access logs
tail -f /opt/homebrew/var/log/nginx/access.log

# nginx error logs
tail -f /opt/homebrew/var/log/nginx/error.log

# Cloudflare tunnel logs
cloudflared tunnel run sparkify  # Shows real-time logs
```

## Troubleshooting Guide

### Common Issues

#### 1. 522 Connection Timeout
**Symptoms**: Cloudflare returns 522 error
**Causes**: 
- nginx not running
- Tunnel can't reach nginx
- Firewall blocking local connections

**Solutions**:
```bash
# Check nginx status
sudo nginx -t && sudo nginx -s reload

# Check tunnel connectivity to local nginx (origin over HTTP)
curl -I http://localhost:80

# Restart tunnel system service
sudo launchctl stop com.cloudflare.sparkify
sudo launchctl start com.cloudflare.sparkify
```

#### 2. DNS Not Resolving
**Symptoms**: Domain doesn't resolve to Cloudflare IPs
**Solutions**:
```bash
# Check DNS propagation
dig +short sparkify.dev @8.8.8.8

# Verify Cloudflare DNS settings
# Ensure CNAME record: sparkify.dev → 392abfe9-a2db-41dd-a688-69e887823cdc.cfargotunnel.com
```

#### 3. SSL Certificate Issues
**Symptoms**: SSL warnings or connection errors
**Solutions**:
```bash
# Regenerate local certificates
mkcert sparkify.dev

# Update nginx config with new cert paths
sudo nginx -t && sudo nginx -s reload
```

#### 4. Performance Issues
**Symptoms**: Slow response times
**Diagnostics**:
```bash
# Check nginx worker processes
ps aux | grep nginx

# Test response times
curl -w "@curl-format.txt" -o /dev/null -s https://sparkify.dev

# Check rate limiting
grep "limiting requests" /opt/homebrew/var/log/nginx/error.log
```

### Performance Monitoring
```bash
# nginx status (if enabled)
curl http://localhost/nginx_status

# Cloudflare tunnel metrics
curl http://127.0.0.1:20241/metrics

# System resources
top -pid $(pgrep nginx)
```

## Security Considerations

### Network Security
- **No Open Ports**: Tunnel uses outbound connections only
- **Cloudflare WAF**: Automatic protection against common attacks
- **Rate Limiting**: Configured per endpoint type

### SSL/TLS Security
- **TLS at Edge**: Cloudflare provides TLS 1.2/1.3 and modern ciphers
- **Origin**: HTTP-only; no HSTS configured locally

### Application Security
- **CSP Headers**: Content Security Policy implemented
- **XSS Protection**: Cross-site scripting prevention
- **Frame Options**: Clickjacking protection
- **Server Tokens**: nginx version hidden

## Development vs Production

### Local Development
- Use `mac-deploy.sh` for local setup
- Connects to `http://localhost:4200` and `http://localhost:5001`
- No local TLS; Cloudflare handles HTTPS for the public domain
- Domain resolves via `/etc/hosts`

### Production Deployment
- Cloudflare Tunnel for public access
- Uses `localhost` for tunnel connectivity to local nginx (robust to LAN IP changes)
- Cloudflare manages SSL termination
- DNS managed by Cloudflare

## Future Enhancements

### Monitoring
- Implement nginx status endpoint
- Add Prometheus metrics export
- Set up log aggregation with Grafana

### High Availability
- Configure multiple tunnel connections
- Implement health checks for automatic failover
- Add backup tunnel routes

### Performance
- Enable nginx caching for static assets
- Implement connection pooling optimizations
- Add Cloudflare Workers for edge computing

## Support & Resources

### Documentation
- [Cloudflare Tunnel Docs](https://developers.cloudflare.com/cloudflare-one/connections/connect-apps/)
- [nginx Configuration Reference](https://nginx.org/en/docs/)
- [CLAUDE.md](./CLAUDE.md) - Project-specific setup instructions

### Configuration Files
- `/opt/homebrew/etc/nginx/nginx.conf` - nginx configuration
- `/Users/antbly/.cloudflared/config.yml` - Tunnel configuration
- `mac-deploy.sh` - Automated deployment script

### Key Commands
```bash
# Health check
curl https://sparkify.dev/api/health

# Performance test
curl -w "%{time_total}\\n" -o /dev/null -s https://sparkify.dev

# Tunnel status
cloudflared tunnel info 392abfe9-a2db-41dd-a688-69e887823cdc
```

---

**Live Site**: https://sparkify.dev  
**Setup Date**: August 17, 2025  
**Last Updated**: August 17, 2025
