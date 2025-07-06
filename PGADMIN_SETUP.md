# pgAdmin Configuration

This project includes a pre-configured pgAdmin setup for easy database management during development.

## Features

- **Auto-login**: No need to enter credentials every time
- **Pre-configured server**: PostgreSQL server is automatically registered
- **Development-friendly**: Login requirements disabled for convenience

## Configuration

The pgAdmin setup is configured through:

1. **Environment Variables** in `docker-compose.yml`:
   - `PGADMIN_CONFIG_LOGIN_REQUIRED: 'False'` - Disables login requirement
   - `PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED: 'False'` - Disables master password
   - `PGADMIN_CONFIG_AUTO_LOGIN: 'True'` - Enables auto-login

2. **Server Configuration** in `servers.json`:
   - Pre-configured connection to the PostgreSQL container
   - Host: `postgres` (Docker service name)
   - Port: `5432`
   - Database: `myapp`
   - Username: `postgres`

## Usage

1. Start the services:
   ```bash
   docker-compose up -d
   ```

2. Access pgAdmin:
   - URL: http://localhost:8080
   - No login required - you'll be automatically logged in
   - The PostgreSQL server will be pre-configured and ready to use

## Default Credentials

If login is ever required:
- **Email**: admin@example.com
- **Password**: admin_password

## Server Details

- **Name**: Local PostgreSQL
- **Host**: postgres
- **Port**: 5432
- **Database**: myapp
- **Username**: postgres
- **Password**: postgres_password (from PostgreSQL container)

## Security Note

This configuration is designed for development environments only. The auto-login and disabled security features should not be used in production. 