#!/bin/bash

# SSH into Oracle Cloud VM for deployment
HOST=146.235.218.2
USER=ubuntu
KEY=~/.ssh/ssh-oracle.key

# Print info
cat <<EOF
========================================
Connecting to Oracle Cloud VM ($USER@$HOST)
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