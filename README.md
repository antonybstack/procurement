# PostgreSQL Docker Compose Setup with High Availability

This setup provides a production-ready PostgreSQL database with high availability features, optimized for development on macOS with OrbStack.

## Features

- **High Availability**: Auto-restart policy with health checks
- **Performance Optimized**: Tuned PostgreSQL configuration for development workloads
- **Monitoring**: Built-in pg_stat_statements for query performance tracking
- **Web Interface**: Optional pgAdmin for database management
- **Persistence**: Data volumes for data persistence across container restarts
- **Resource Management**: CPU and memory limits to prevent resource exhaustion

## Prerequisites

- [OrbStack](https://orbstack.dev/) installed and running
- Docker Compose (included with OrbStack)

## Quick Start

1. **Start the services:**
   ```bash
   docker-compose up -d
   ```

2. **Check service status:**
   ```bash
   docker-compose ps
   ```

3. **View logs:**
   ```bash
   docker-compose logs -f postgres
   ```

## Connection Details

### PostgreSQL Database
- **Host**: `localhost` or `127.0.0.1`
- **Port**: `5432`
- **Database**: `myapp`
- **Username**: `postgres`
- **Password**: `postgres_password`

### pgAdmin Web Interface (Optional)
- **URL**: http://localhost:8080
- **Email**: `admin@example.com`
- **Password**: `admin_password`

#### When adding a new server in pgAdmin, use these connection details:
- Host name/address: postgres (not localhost or 127.0.0.1)
- Port: 5432
- Maintenance database: postgres
- Username: postgres
- Password: postgres_password
> Inside the Docker network, containers can communicate using their service names
  postgres is the service name defined in the docker-compose.yml
  localhost or 127.0.0.1 refers to the pgAdmin container itself, not the PostgreSQL container

## High Availability Features

### Auto-Restart Policy
- **`restart: unless-stopped`**: Container automatically restarts unless manually stopped
- **Health Checks**: Monitors database readiness every 30 seconds
- **Graceful Startup**: 40-second startup period before health checks begin

### Health Monitoring
The setup includes comprehensive health monitoring:
- Database connectivity checks
- Automatic restart on failures
- Resource usage monitoring
- Query performance tracking

## Configuration

### Environment Variables
You can customize the setup by modifying the environment variables in `docker-compose.yml`:

```yaml
environment:
  POSTGRES_DB: myapp          # Database name
  POSTGRES_USER: postgres     # Database user
  POSTGRES_PASSWORD: postgres_password  # Database password
```

### PostgreSQL Configuration
The `postgresql.conf` file contains optimized settings for:
- Memory management
- Connection pooling
- Write-Ahead Logging (WAL)
- Query performance
- Logging and monitoring

### Resource Limits
- **Memory**: 1GB limit, 512MB reservation
- **CPU**: 1 core limit, 0.5 core reservation

## Management Commands

### Start Services
```bash
docker-compose up -d
```

### Stop Services (Restart the PostgreSQL container to apply the new schema and data)
```bash
docker-compose down --remove-orphans
```

### Restart Services
```bash
docker-compose restart
```

### View Logs
```bash
# All services
docker-compose logs -f

# PostgreSQL only
docker-compose logs -f postgres

# pgAdmin only
docker-compose logs -f pgadmin
```

### Access PostgreSQL Shell
```bash
docker-compose exec postgres psql -U postgres -d myapp
```

### Backup Database
```bash
docker-compose exec postgres pg_dump -U postgres myapp > backup.sql
```

### Restore Database
```bash
docker-compose exec -T postgres psql -U postgres -d myapp < backup.sql
```

## Data Persistence

Data is stored in Docker volumes:
- `postgres_data`: PostgreSQL database files
- `pgadmin_data`: pgAdmin configuration and data

To backup volumes:
```bash
docker run --rm -v postgres_data:/data -v $(pwd):/backup alpine tar czf /backup/postgres_data.tar.gz -C /data .
```

## Reset Database (Development Only)

⚠️ **WARNING**: This will delete ALL database data!

If you need to reset the database for development/testing:

```bash
./reset-db.sh
```

This script will:
1. Stop all database containers
2. Remove all data volumes
3. Start fresh with initial schema and sample data

## Manual Volume Management

For advanced users, you can manually manage volumes:

```bash
# List volumes
docker volume ls

# Remove specific volumes (WARNING: This deletes data!)
docker volume rm postgres_data pgadmin_data ollama_data
```

## Ollama Management

### Normal Operations (Data Preserved)
```bash
./start-ollama.sh          # Start Ollama with model pulling
./restart-ollama.sh        # Restart Ollama (preserves models)
```

### Reset Operations (Data Removed)
```bash
./reset-ollama.sh          # Reset Ollama (removes all models)
```

### Troubleshooting Ollama
```bash
# Check if Ollama is running
curl http://localhost:11434/api/tags

# Check Ollama logs
docker-compose -f docker-compose.ollama.yml logs ollama

# Pull a specific model
curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.1:1b"}'
```

## Health Checks
### Check the current status of your PostgreSQL container and see what's happening.
``` bash
docker-compose ps
```

### Check the container logs to see if there are any issues.
``` bash
docker-compose logs postgres
```

### Test the connection directly from the host to see if the port is accessible.
``` bash
nc -zv localhost 5432
```

### Test the PostgreSQL connection directly.
``` bash
nc -zv localhost 5432
```

### pgAdmin logs.
``` bash
docker-compose logs pgadmin
```

### Test the connection from within the pgAdmin container to see what hostname it should use:
``` bash
docker-compose exec pgadmin ping -c 1 postgres
```

## Troubleshooting

### Container Won't Start
1. Check if port 5432 is already in use:
   ```bash
   lsof -i :5432
   ```

2. Check container logs:
   ```bash
   docker-compose logs postgres
   ```

### Connection Issues
1. Verify the container is running:
   ```bash
   docker-compose ps
   ```

2. Check health status:
   ```bash
   docker-compose exec postgres pg_isready -U postgres
   ```

### Performance Issues
1. Monitor resource usage:
   ```bash
   docker stats postgres_db
   ```

2. Check query performance:
   ```bash
   docker-compose exec postgres psql -U postgres -d myapp -c "SELECT * FROM pg_stat_statements ORDER BY total_time DESC LIMIT 10;"
   ```

## Security Considerations

⚠️ **Important**: This setup is configured for development use. For production:

1. Change default passwords
2. Enable SSL/TLS
3. Restrict network access
4. Use secrets management
5. Implement proper backup strategies
6. Configure firewall rules

## Customization

### Adding Custom Extensions
Edit `database-schema.sql` to add PostgreSQL extensions:
```sql
CREATE EXTENSION IF NOT EXISTS "your_extension_name";
```

### Modifying PostgreSQL Settings
Edit `postgresql.conf` and restart the container:
```bash
docker-compose restart postgres
```

### Adding More Services
Add additional services to `docker-compose.yml` as needed for your application stack.

## Support

For issues related to:
- **OrbStack**: Visit [OrbStack Documentation](https://docs.orbstack.dev/)
- **PostgreSQL**: Visit [PostgreSQL Documentation](https://www.postgresql.org/docs/)
- **Docker Compose**: Visit [Docker Compose Documentation](https://docs.docker.com/compose/) 