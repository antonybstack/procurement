#!/bin/bash

# Setup swap memory for low-RAM servers
echo "Setting up swap memory..."

# Check current swap status
echo "Current memory status:"
free -h
echo ""

# Check if swap already exists
if swapon --show | grep -q "/swapfile"; then
    echo "Swap file already exists. Current swap status:"
    swapon --show
    exit 0
fi

# Calculate swap size (2x RAM, minimum 1GB, maximum 4GB)
RAM_KB=$(grep MemTotal /proc/meminfo | awk '{print $2}')
RAM_MB=$((RAM_KB / 1024))
SWAP_MB=$((RAM_MB * 2))

# Limit swap size
if [ $SWAP_MB -lt 1024 ]; then
    SWAP_MB=1024  # Minimum 1GB
elif [ $SWAP_MB -gt 4096 ]; then
    SWAP_MB=4096  # Maximum 4GB
fi

echo "RAM: ${RAM_MB}MB"
echo "Setting up ${SWAP_MB}MB swap file..."

# Create swap file
fallocate -l ${SWAP_MB}M /swapfile

# Set correct permissions
chmod 600 /swapfile

# Make it swap
mkswap /swapfile

# Enable swap
swapon /swapfile

# Make swap permanent
echo '/swapfile none swap sw 0 0' >> /etc/fstab

# Set swappiness (how aggressively to use swap)
echo 'vm.swappiness=10' >> /etc/sysctl.conf
sysctl vm.swappiness=10

echo ""
echo "Swap setup complete!"
echo "New memory status:"
free -h
echo ""
echo "Swap file details:"
swapon --show 