#!/bin/bash

echo "🚨 EMERGENCY DISK CLEANUP - Critical Space Recovery"
echo "=================================================="

echo "📊 Current disk usage:"
df -h / | head -2

echo ""
echo "🧹 Step 1: Aggressive Docker cleanup..."
# Stop all containers first
docker stop $(docker ps -aq) 2>/dev/null || true

# Remove all containers
docker rm $(docker ps -aq) 2>/dev/null || true

# Remove all images
docker rmi $(docker images -aq) 2>/dev/null || true

# Remove all volumes
docker volume rm $(docker volume ls -q) 2>/dev/null || true

# Remove all networks (except defaults)
docker network rm $(docker network ls -q) 2>/dev/null || true

# System prune with force
docker system prune -af --volumes

echo ""
echo "🗑️  Step 2: Clear NuGet caches..."
rm -rf /root/.nuget/packages/* 2>/dev/null || true
rm -rf /tmp/nuget-scratch 2>/dev/null || true

echo ""
echo "📝 Step 3: Clear system logs..."
journalctl --vacuum-size=50M
rm -rf /var/log/*.log.* 2>/dev/null || true
rm -rf /var/log/*/*.log.* 2>/dev/null || true

echo ""
echo "🧽 Step 4: Clear temporary files..."
rm -rf /tmp/* 2>/dev/null || true
rm -rf /var/tmp/* 2>/dev/null || true

echo ""
echo "📦 Step 5: Clear package caches..."
apt clean 2>/dev/null || true
apt autoremove -y 2>/dev/null || true

echo ""
echo "📊 Disk space after cleanup:"
df -h / | head -2

echo ""
echo "✅ Emergency cleanup complete!"
