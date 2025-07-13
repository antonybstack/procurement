#!/bin/bash

# SSH into Digital Ocean Droplet for deployment
HOST=24.199.78.31
USER=root
KEY=~/.ssh/ssh-oracle.key

# Print info
cat <<EOF
========================================
Connecting to Digital Ocean Droplet ($USER@$HOST)
- SSH key: $KEY
- User: $USER
- Host: $HOST

Once connected, you can:
  1. Clone your repo to /opt/procurement
  2. Run ./start.sh to deploy containers
  3. Use procurement-status to check status
========================================
EOF

ssh -i $KEY $USER@$HOST 