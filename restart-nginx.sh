#!/bin/bash

# Restart nginx LaunchDaemon with updated configuration

echo "🔄 Testing nginx configuration..."
if ! sudo nginx -t; then
    echo "❌ nginx configuration test failed. Aborting."
    exit 1
fi

echo "🔄 Restarting nginx system service..."
sudo launchctl stop dev.sparkify.nginx
sudo launchctl start dev.sparkify.nginx

echo "✅ nginx LaunchDaemon restarted successfully!"
