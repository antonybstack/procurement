#!/bin/bash

# Oracle Cloud VM Setup Script for Procurement System
# This script sets up a fresh Oracle Cloud VM instance for running the procurement system

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

warn() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
}

info() {
    echo -e "${BLUE}[$(date +'%Y-%m-%d %H:%M:%S')] INFO: $1${NC}"
}

# Check if running as root
if [[ $EUID -eq 0 ]]; then
   error "This script should not be run as root. Please run as a regular user with sudo privileges."
   exit 1
fi

# Check if sudo is available
if ! command -v sudo &> /dev/null; then
    error "sudo is not installed. Please install sudo first."
    exit 1
fi

log "Starting Oracle Cloud VM setup for Procurement System..."

# Step 1: System Update and Upgrade
log "Step 1: Updating system packages..."
sudo apt update
sudo apt upgrade -y
sudo apt autoremove -y
sudo apt autoclean

# Step 2: Install essential packages
log "Step 2: Installing essential packages..."
sudo apt install -y \
    curl \
    wget \
    git \
    unzip \
    software-properties-common \
    apt-transport-https \
    ca-certificates \
    gnupg \
    lsb-release \
    htop \
    vim \
    ufw \
    fail2ban \
    nginx \
    certbot \
    python3-certbot-nginx

# Step 3: Install Docker
log "Step 3: Installing Docker..."
# Remove old versions if they exist
sudo apt remove -y docker docker-engine docker.io containerd runc 2>/dev/null || true

# Add Docker's official GPG key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Add Docker repository
echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Update package index
sudo apt update

# Install Docker
sudo apt install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin

# Add current user to docker group
sudo usermod -aG docker $USER

# Start and enable Docker
sudo systemctl start docker
sudo systemctl enable docker

# Step 4: Install Docker Compose (standalone version for compatibility)
log "Step 4: Installing Docker Compose..."
sudo curl -L "https://github.com/docker/compose/releases/latest/download/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
sudo chmod +x /usr/local/bin/docker-compose

# Step 5: Configure Firewall (UFW)
log "Step 5: Configuring firewall..."
# Reset UFW to default
sudo ufw --force reset

# Set default policies
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH (important!)
sudo ufw allow ssh

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Allow application ports
sudo ufw allow 4200/tcp  # Angular Frontend
sudo ufw allow 5001/tcp  # .NET API
sudo ufw allow 8080/tcp  # pgAdmin
sudo ufw allow 3000/tcp  # Grafana
sudo ufw allow 5601/tcp  # Kibana
sudo ufw allow 9090/tcp  # Prometheus
sudo ufw allow 3100/tcp  # Loki

# Enable UFW
sudo ufw --force enable

# Step 6: Configure Fail2ban
log "Step 6: Configuring Fail2ban..."
sudo systemctl enable fail2ban
sudo systemctl start fail2ban

# Create custom fail2ban configuration
sudo tee /etc/fail2ban/jail.local > /dev/null <<EOF
[DEFAULT]
bantime = 3600
findtime = 600
maxretry = 3

[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log
maxretry = 3
bantime = 3600

[nginx-http-auth]
enabled = true
filter = nginx-http-auth
port = http,https
logpath = /var/log/nginx/error.log
maxretry = 3
bantime = 3600
EOF

# Restart fail2ban
sudo systemctl restart fail2ban

# Step 7: Configure Nginx as reverse proxy (optional but recommended)
log "Step 7: Configuring Nginx reverse proxy..."
sudo tee /etc/nginx/sites-available/procurement > /dev/null <<EOF
server {
    listen 80;
    server_name _;

    # Frontend
    location / {
        proxy_pass http://localhost:4200;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }

    # API
    location /api/ {
        proxy_pass http://localhost:5001/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }

    # pgAdmin
    location /pgadmin/ {
        proxy_pass http://localhost:8080/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }

    # Grafana
    location /grafana/ {
        proxy_pass http://localhost:3000/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }

    # Kibana
    location /kibana/ {
        proxy_pass http://localhost:5601/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade \$http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host \$host;
        proxy_set_header X-Real-IP \$remote_addr;
        proxy_set_header X-Forwarded-For \$proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto \$scheme;
        proxy_cache_bypass \$http_upgrade;
    }
}
EOF

# Enable the site
sudo ln -sf /etc/nginx/sites-available/procurement /etc/nginx/sites-enabled/
sudo rm -f /etc/nginx/sites-enabled/default

# Test nginx configuration
sudo nginx -t

# Restart nginx
sudo systemctl restart nginx
sudo systemctl enable nginx

# Step 8: Create application directory and clone repository
log "Step 8: Setting up application directory..."
APP_DIR="/opt/procurement"
sudo mkdir -p $APP_DIR
sudo chown $USER:$USER $APP_DIR

# Step 9: Create systemd service for auto-start (optional)
log "Step 9: Creating systemd service for auto-start..."
sudo tee /etc/systemd/system/procurement.service > /dev/null <<EOF
[Unit]
Description=Procurement System
Requires=docker.service
After=docker.service

[Service]
Type=oneshot
RemainAfterExit=yes
WorkingDirectory=$APP_DIR
ExecStart=/bin/bash -c 'cd $APP_DIR && ./start.sh'
ExecStop=/bin/bash -c 'cd $APP_DIR && ./stop.sh'
User=$USER
Group=$USER

[Install]
WantedBy=multi-user.target
EOF

# Step 10: Create monitoring script
log "Step 10: Creating monitoring script..."
sudo tee /usr/local/bin/procurement-status > /dev/null <<EOF
#!/bin/bash
echo "=== Procurement System Status ==="
echo "Docker containers:"
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
echo ""
echo "System resources:"
free -h
echo ""
echo "Disk usage:"
df -h /
echo ""
echo "Firewall status:"
sudo ufw status
EOF

sudo chmod +x /usr/local/bin/procurement-status

# Step 11: Create backup script
log "Step 11: Creating backup script..."
sudo tee /usr/local/bin/procurement-backup > /dev/null <<EOF
#!/bin/bash
BACKUP_DIR="/opt/backups/procurement"
DATE=\$(date +%Y%m%d_%H%M%S)
mkdir -p \$BACKUP_DIR

echo "Creating backup: \$BACKUP_DIR/backup_\$DATE.tar.gz"

# Stop services
cd /opt/procurement
./stop.sh

# Create backup
sudo tar -czf \$BACKUP_DIR/backup_\$DATE.tar.gz \\
    -C /opt/procurement \\
    --exclude='node_modules' \\
    --exclude='.git' \\
    .

# Restart services
./start.sh

echo "Backup completed: \$BACKUP_DIR/backup_\$DATE.tar.gz"
EOF

sudo chmod +x /usr/local/bin/procurement-backup

# Step 12: Set up log rotation
log "Step 12: Setting up log rotation..."
sudo tee /etc/logrotate.d/procurement > /dev/null <<EOF
/opt/procurement/logs/*.log {
    daily
    missingok
    rotate 7
    compress
    delaycompress
    notifempty
    create 644 $USER $USER
}
EOF

# Step 13: Create environment file
log "Step 13: Creating environment configuration..."
sudo tee $APP_DIR/config.env > /dev/null <<EOF
# Procurement System Environment Configuration
# Generated on $(date)

# Database Configuration
POSTGRES_DB=myapp
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres_password

# Elasticsearch Configuration
ELASTIC_PASSWORD=elastic_password
KIBANA_PASSWORD=kibana_password

# Application URLs (update with your VM's public IP)
FRONTEND_URL=http://$(curl -s ifconfig.me):4200
API_URL=http://$(curl -s ifconfig.me):5001
PGADMIN_URL=http://$(curl -s ifconfig.me):8080
GRAFANA_URL=http://$(curl -s ifconfig.me):3000
KIBANA_URL=http://$(curl -s ifconfig.me):5601

# Security
# Change these passwords in production!
PGADMIN_EMAIL=admin@example.com
PGADMIN_PASSWORD=admin_password
GRAFANA_PASSWORD=admin
EOF

sudo chown $USER:$USER $APP_DIR/config.env

# Step 14: Final system optimization
log "Step 14: Optimizing system settings..."

# Increase file descriptor limits
sudo tee -a /etc/security/limits.conf > /dev/null <<EOF
* soft nofile 65536
* hard nofile 65536
EOF

# Optimize Docker daemon
sudo tee /etc/docker/daemon.json > /dev/null <<EOF
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  },
  "storage-driver": "overlay2",
  "storage-opts": [
    "overlay2.override_kernel_check=true"
  ]
}
EOF

# Restart Docker to apply changes
sudo systemctl restart docker

# Step 15: Create setup completion script
log "Step 15: Creating setup completion instructions..."
sudo tee $APP_DIR/SETUP_COMPLETE.md > /dev/null <<EOF
# Oracle Cloud VM Setup Complete! 🎉

## Next Steps:

1. **Clone your repository:**
   \`\`\`bash
   cd /opt/procurement
   git clone <your-repo-url> .
   \`\`\`

2. **Start the system:**
   \`\`\`bash
   ./start.sh
   \`\`\`

3. **Check status:**
   \`\`\`bash
   procurement-status
   \`\`\`

## Access URLs:
- **Frontend**: http://$(curl -s ifconfig.me):4200
- **API**: http://$(curl -s ifconfig.me):5001
- **Swagger**: http://$(curl -s ifconfig.me):5001/swagger
- **pgAdmin**: http://$(curl -s ifconfig.me):8080
- **Grafana**: http://$(curl -s ifconfig.me):3000
- **Kibana**: http://$(curl -s ifconfig.me):5601

## Default Credentials:
- **pgAdmin**: admin@example.com / admin_password
- **Grafana**: admin / admin

## Useful Commands:
- \`procurement-status\` - Check system status
- \`procurement-backup\` - Create backup
- \`sudo ufw status\` - Check firewall
- \`sudo fail2ban-client status\` - Check fail2ban

## Security Notes:
- Change default passwords immediately
- Consider setting up SSL certificates
- Monitor logs regularly
- Keep system updated

## Oracle Cloud Ingress Rules Required:
Make sure to add these ingress rules in Oracle Cloud Console:
- Port 4200 (Frontend)
- Port 5001 (API)
- Port 8080 (pgAdmin)
- Port 3000 (Grafana)
- Port 5601 (Kibana)
- Port 9090 (Prometheus)
- Port 3100 (Loki)

Setup completed on: $(date)
EOF

sudo chown $USER:$USER $APP_DIR/SETUP_COMPLETE.md

# Final summary
log "Setup completed successfully!"
echo ""
echo "=========================================="
echo "🎉 Oracle Cloud VM Setup Complete!"
echo "=========================================="
echo ""
echo "📋 Summary of what was installed:"
echo "   ✅ System packages updated"
echo "   ✅ Docker and Docker Compose"
echo "   ✅ UFW firewall configured"
echo "   ✅ Fail2ban security"
echo "   ✅ Nginx reverse proxy"
echo "   ✅ System monitoring tools"
echo "   ✅ Backup scripts"
echo "   ✅ Log rotation"
echo ""
echo "🔧 Next steps:"
echo "   1. Clone your repository to /opt/procurement"
echo "   2. Run: ./start.sh"
echo "   3. Check status: procurement-status"
echo ""
echo "🌐 Your VM's public IP: $(curl -s ifconfig.me)"
echo ""
echo "📖 See /opt/procurement/SETUP_COMPLETE.md for detailed instructions"
echo ""
echo "⚠️  IMPORTANT: Change default passwords and set up SSL for production!"
echo "=========================================="

# Remind user to log out and back in for docker group
warn "You need to log out and log back in for Docker group permissions to take effect."
warn "Or run: newgrp docker"

log "Setup script completed successfully!" 