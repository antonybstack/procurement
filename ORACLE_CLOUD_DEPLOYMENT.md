# Oracle Cloud VM Deployment Guide

This guide will help you deploy the procurement system on an Oracle Cloud VM instance.

## Prerequisites

- Oracle Cloud account with a VM instance running Ubuntu 20.04 or later
- SSH access to your VM
- Basic knowledge of Linux commands

## Step 1: Create Oracle Cloud VM Instance

1. **Log into Oracle Cloud Console**
2. **Create a VM Instance:**
   - **Name**: `procurement-vm`
   - **Image**: Canonical Ubuntu 20.04 or later
   - **Shape**: VM.Standard.A1.Flex (2 OCPU, 12 GB RAM minimum)
   - **Network**: Create new VCN or use existing
   - **Subnet**: Public subnet
   - **Public IP**: Yes

3. **Configure Security Lists:**
   Add these ingress rules to your subnet's security list:
   ```
   Port 22 (SSH) - Source: 0.0.0.0/0
   Port 80 (HTTP) - Source: 0.0.0.0/0
   Port 443 (HTTPS) - Source: 0.0.0.0/0
   Port 4200 (Frontend) - Source: 0.0.0.0/0
   Port 5001 (API) - Source: 0.0.0.0/0
   Port 8080 (pgAdmin) - Source: 0.0.0.0/0
   Port 3000 (Grafana) - Source: 0.0.0.0/0
   Port 5601 (Kibana) - Source: 0.0.0.0/0
   Port 9090 (Prometheus) - Source: 0.0.0.0/0
   Port 3100 (Loki) - Source: 0.0.0/0
   ```

## Step 2: Connect to Your VM

```bash
ssh ubuntu@YOUR_VM_PUBLIC_IP
```

## Step 3: Run the Setup Script

1. **Download the setup script:**
   ```bash
   wget https://raw.githubusercontent.com/YOUR_USERNAME/YOUR_REPO/main/setup-oracle-vm.sh
   ```

2. **Make it executable:**
   ```bash
   chmod +x setup-oracle-vm.sh
   ```

3. **Run the setup script:**
   ```bash
   ./setup-oracle-vm.sh
   ```

   This script will:
   - Update system packages
   - Install Docker and Docker Compose
   - Configure firewall (UFW)
   - Set up security with Fail2ban
   - Configure Nginx reverse proxy
   - Create monitoring and backup scripts
   - Optimize system settings

## Step 4: Deploy Your Application

1. **Clone your repository:**
   ```bash
   cd /opt/procurement
   git clone https://github.com/YOUR_USERNAME/YOUR_REPO.git .
   ```

2. **Start the system:**
   ```bash
   ./start.sh
   ```

3. **Check status:**
   ```bash
   procurement-status
   ```

## Step 5: Access Your Application

Once deployed, you can access your services at:

- **Frontend**: `http://YOUR_VM_PUBLIC_IP:4200`
- **API**: `http://YOUR_VM_PUBLIC_IP:5001`
- **Swagger**: `http://YOUR_VM_PUBLIC_IP:5001/swagger`
- **pgAdmin**: `http://YOUR_VM_PUBLIC_IP:8080`
- **Grafana**: `http://YOUR_VM_PUBLIC_IP:3000`
- **Kibana**: `http://YOUR_VM_PUBLIC_IP:5601`

## Default Credentials

- **pgAdmin**: `admin@example.com` / `admin_password`
- **Grafana**: `admin` / `admin`

⚠️ **IMPORTANT**: Change these passwords immediately after deployment!

## Useful Commands

```bash
# Check system status
procurement-status

# Create backup
procurement-backup

# View logs
docker-compose -f docker-compose.api.yml logs -f
docker-compose -f docker-compose.frontend.yml logs -f

# Restart services
./restart-api.sh
./rebuild-frontend.sh

# Stop all services
./stop.sh

# Start all services
./start.sh
```

## Security Recommendations

1. **Change Default Passwords:**
   - Update pgAdmin password
   - Update Grafana password
   - Update database passwords

2. **Set Up SSL (Recommended):**
   ```bash
   sudo certbot --nginx -d your-domain.com
   ```

3. **Restrict Access:**
   - Update firewall rules to limit access to specific IPs
   - Use VPN for admin access

4. **Regular Maintenance:**
   - Keep system updated: `sudo apt update && sudo apt upgrade`
   - Monitor logs regularly
   - Create regular backups

## Troubleshooting

### Common Issues

1. **Docker permission denied:**
   ```bash
   # Log out and log back in, or run:
   newgrp docker
   ```

2. **Port already in use:**
   ```bash
   # Check what's using the port
   sudo netstat -tulpn | grep :PORT_NUMBER
   ```

3. **Services not starting:**
   ```bash
   # Check logs
   docker-compose logs SERVICE_NAME
   ```

4. **Firewall blocking access:**
   ```bash
   # Check firewall status
   sudo ufw status
   ```

### Getting Help

- Check the logs in `/opt/procurement/logs/`
- Use `procurement-status` to see system health
- Review Docker container logs
- Check nginx error logs: `sudo tail -f /var/log/nginx/error.log`

## Backup and Recovery

### Create Backup
```bash
procurement-backup
```

### Restore from Backup
```bash
cd /opt/procurement
./stop.sh
sudo tar -xzf /opt/backups/procurement/backup_YYYYMMDD_HHMMSS.tar.gz
./start.sh
```

## Monitoring

The system includes several monitoring tools:

- **Grafana**: Main dashboard for metrics and logs
- **Prometheus**: Metrics collection
- **Loki**: Log aggregation
- **Kibana**: Elasticsearch visualization

Access these at the URLs listed above.

## Cost Optimization

For Oracle Cloud Free Tier:
- Use VM.Standard.A1.Flex shape
- Monitor resource usage
- Consider stopping non-essential services when not in use
- Use object storage for backups (cheaper than block storage)

## Next Steps

1. Set up a domain name and SSL certificates
2. Configure automated backups
3. Set up monitoring alerts
4. Implement CI/CD pipeline
5. Add load balancing for high availability

---

**Need Help?** Check the logs and use the troubleshooting commands above. For additional support, refer to the application documentation or create an issue in the repository. 