# Local Development Setup

This guide explains how to set up local development with Docker services (database, pgAdmin) and run the API locally in your debugger.

## Quick Start

### 1. Start the Development Environment (Database Only)

```bash
# Start PostgreSQL and pgAdmin (no API)
docker-compose -f docker-compose.dev-local.yml up -d

# Check that services are running
docker-compose -f docker-compose.dev-local.yml ps
```

### 2. Run the API Locally

```bash
# Navigate to the API directory
cd ProcurementAPI

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API locally
dotnet run
```

### 3. Debug in VS Code

1. Open VS Code in the project root
2. Set breakpoints in your C# code
3. Press F5 and select "Launch API Locally (Debug)"
4. The API will start and automatically open Swagger UI

## Development Workflow

### Starting Development

```bash
# Start the development environment (database + pgAdmin)
docker-compose -f docker-compose.dev-local.yml up -d

# In another terminal, run the API locally
cd ProcurementAPI
dotnet run
```

### Making Code Changes

1. **Edit your C# code** in VS Code
2. **Save the file** - the API will automatically restart (hot reload)
3. **Set breakpoints** and debug as needed
4. **Test changes** at http://localhost:5001/swagger

### Stopping Development

```bash
# Stop the API (Ctrl+C in the terminal where it's running)

# Stop the development environment
docker-compose -f docker-compose.dev-local.yml down
```

## VS Code Debugging

### Available Debug Configurations

1. **Launch API Locally (Debug)** - Standard debugging with build
2. **Launch API Locally (Hot Reload)** - Faster startup, no pre-build
3. **Debug API in Docker Container** - Debug API running in container
4. **Attach to Running Container** - Alternative container debugging

### Using VS Code Tasks

1. **Start Development Environment**: `Ctrl+Shift+P` → "Tasks: Run Task" → "start-dev-environment"
2. **Stop Development Environment**: `Ctrl+Shift+P` → "Tasks: Run Task" → "stop-dev-environment"
3. **Build API**: `Ctrl+Shift+P` → "Tasks: Run Task" → "build-api"

## Configuration

### Connection String

The local API uses this connection string to connect to the Docker PostgreSQL:

```
Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=postgres_password
```

### Environment Variables

The local API runs with these environment variables:
- `ASPNETCORE_ENVIRONMENT=Development`
- `ASPNETCORE_URLS=http://localhost:5001`
- `ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=postgres_password;Include Error Detail=true`

## Services Available

### PostgreSQL Database
- **Host**: localhost
- **Port**: 5432
- **Database**: myapp
- **Username**: postgres
- **Password**: postgres_password

### pgAdmin (Optional)
- **URL**: http://localhost:8080
- **Email**: admin@example.com
- **Password**: admin_password

### API (Local)
- **URL**: http://localhost:5001
- **Swagger UI**: http://localhost:5001/swagger

### Grafana Observability Stack (Optional)
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **Loki**: http://localhost:3100
- **Tempo**: http://localhost:3200
- **Promtail**: http://localhost:9080
- **OTEL Collector**: http://localhost:8888

## Benefits of Local Development

### ✅ **Full Debugging Support**
- Set breakpoints in VS Code
- Step through code line by line
- Inspect variables and call stack
- Hot reload for fast development

### ✅ **Fast Development Cycle**
- No container rebuilds needed
- Instant code changes
- Direct file system access
- Native .NET debugging

### ✅ **Database Access**
- PostgreSQL running in Docker
- pgAdmin for database management
- Persistent data across restarts
- Same data as production environment

### ✅ **Isolated Services**
- Only run what you need
- Database and pgAdmin in containers
- API runs locally for debugging
- Easy to start/stop services

## Troubleshooting

### Database Connection Issues

```bash
# Check if PostgreSQL is running
docker-compose -f docker-compose.dev-local.yml ps

# Check PostgreSQL logs
docker-compose -f docker-compose.dev-local.yml logs postgres

# Test database connection
docker exec -it postgres_db psql -U postgres -d myapp -c "SELECT 1;"
```

### Port Conflicts

If you get port conflicts:

```bash
# Check what's using the ports
lsof -i :5432  # PostgreSQL
lsof -i :5001  # API
lsof -i :8080  # pgAdmin

# Stop conflicting services
sudo lsof -ti:5432 | xargs kill -9
```

### API Won't Start

```bash
# Check if .NET SDK is installed
dotnet --version

# Restore dependencies
cd ProcurementAPI
dotnet restore

# Clean and rebuild
dotnet clean
dotnet build
```

### Hot Reload Not Working

```bash
# Make sure you're using the Hot Reload configuration
# Or run with watch mode
cd ProcurementAPI
dotnet watch run
```

## Alternative Commands

### Using Docker Compose Directly

```bash
# Start development environment
docker-compose -f docker-compose.dev-local.yml up -d

# View logs
docker-compose -f docker-compose.dev-local.yml logs -f

# Stop environment
docker-compose -f docker-compose.dev-local.yml down

# Rebuild and restart
docker-compose -f docker-compose.dev-local.yml down
docker-compose -f docker-compose.dev-local.yml up -d --build
```

### Using VS Code Terminal

```bash
# Start development environment
docker-compose -f docker-compose.dev-local.yml up -d

# Run API with hot reload
cd ProcurementAPI && dotnet watch run

# Or run API normally
cd ProcurementAPI && dotnet run
```

## Production vs Development

### Development Setup (Local API)
- ✅ API runs locally with full debugging
- ✅ Database runs in Docker
- ✅ Fast development cycle
- ✅ Hot reload support
- ✅ Native .NET debugging

### Production Setup (All in Docker)
- ✅ Everything containerized
- ✅ Consistent environment
- ✅ Easy deployment
- ✅ No local dependencies

## Starting Grafana Observability Stack

To start the Grafana observability stack (optional):

```bash
# Start Grafana stack
./start-grafana.sh

# Or start all services including Grafana
./start.sh
```

The Grafana stack includes:
- **Grafana**: Dashboard and visualization platform
- **Prometheus**: Metrics collection and storage
- **Loki**: Log aggregation
- **Tempo**: Distributed tracing
- **Promtail**: Log shipping
- **OTEL Collector**: OpenTelemetry data collection

## Next Steps

1. **Start the development environment**: `docker-compose -f docker-compose.dev-local.yml up -d`
2. **Run the API locally**: `cd ProcurementAPI && dotnet run`
3. **Set breakpoints** in VS Code
4. **Test the API** at http://localhost:5001/swagger
5. **Debug your code** with full VS Code support
6. **Optional**: Start Grafana stack for monitoring and observability

Happy coding! 🚀 
