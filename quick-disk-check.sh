#!/bin/bash

# Quick disk space diagnostic script
echo "=========================================="
echo "Quick Disk Space Diagnostic"
echo "=========================================="

echo "1. Overall disk usage:"
df -h
echo "" 

echo "2. Docker disk usage:"
docker system df 2>/dev/null || echo "Docker not available"
echo ""
echo "Consider running `docker system prune -a` to clean up unused images and build cache"
echo "Consider running `docker volume rm $(docker volume ls -q)` to clean up unused volumes"
echo ""

echo "3. Top 10 largest directories:"
du -h --max-depth=1 / 2>/dev/null | sort -rh | head -10
echo ""

echo "4. Large files (>500MB):"
find / -type f -size +500M 2>/dev/null | head -10 | while read file; do
    if [ -f "$file" ]; then
        size=$(du -h "$file" 2>/dev/null | cut -f1)
        echo "  $size - $file"
    fi
done
echo ""

echo "5. NuGet cache size:"
du -sh /root/.nuget 2>/dev/null || echo "No .nuget directory found"
echo ""

echo "6. Docker container log sizes:"
find /var/lib/docker/containers/ -name "*.log" -exec du -h {} \; 2>/dev/null | sort -rh | head -5
echo ""

echo "7. System log sizes:"
du -sh /var/log/* 2>/dev/null | sort -rh | head -5
