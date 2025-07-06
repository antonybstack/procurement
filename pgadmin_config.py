# pgAdmin configuration for development
# This file is mounted to /pgadmin4/config_local.py in the container

# Disable master password requirement
MASTER_PASSWORD_REQUIRED = False

# Disable login requirement for development
LOGIN_REQUIRED = False

# Set session timeout to a long duration for development
SESSION_EXPIRATION_TIME = 60 * 60 * 24 * 30  # 30 days

# Disable CSRF protection for development
WTF_CSRF_ENABLED = False

# Allow all hosts
ALLOWED_HOSTS = ['*']

# Disable security headers for development
SECURITY_HEADERS = False

# Disable rate limiting for development
RATE_LIMIT_ENABLED = False

# Set login banner
LOGIN_BANNER = "Development Environment - Auto Login Enabled" 