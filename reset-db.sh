#!/bin/bash

# This script resets the database by removing all data and starting fresh.
# WARNING: This will delete ALL existing data in the database!
# Use this only for development/testing when you need a clean slate.

echo "⚠️  WARNING: This will delete ALL database data!"
echo "This action cannot be undone."
echo ""
read -p "Are you sure you want to reset the database? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    echo "❌ Database reset cancelled."
    exit 0
fi

echo "🧹 Resetting database (removing all data)..."

# Stop and remove existing containers AND volumes
echo "🛑 Stopping containers and removing data volumes..."
docker-compose -f docker-compose.db.yml down -v

# Remove the volumes manually to ensure complete cleanup
echo "🗑️  Removing data volumes..."
docker volume rm postgres_data 2>/dev/null || true

# now call start-db.sh to start fresh
./start-db.sh