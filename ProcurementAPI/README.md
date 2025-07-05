# Procurement RFQ API

A .NET 9 Web API for managing Request for Quote (RFQ) processes in procurement management. This API provides comprehensive endpoints for suppliers, RFQs, quotes, and purchase orders with a modern NSwag UI for testing and documentation.

## Features

- **Entity Framework Core 9.0** with PostgreSQL
- **RESTful API** with comprehensive CRUD operations
- **NSwag/Swagger UI** for interactive API testing and documentation
- **Pagination and Filtering** for all list endpoints
- **CORS Support** for Angular frontend
- **Docker Support** for containerized deployment
- **High Availability** with health checks

## Technology Stack

- **.NET 9.0** - Latest .NET framework
- **Entity Framework Core 9.0** - ORM for database operations
- **PostgreSQL** - Primary database
- **Npgsql** - PostgreSQL provider for EF Core
- **NSwag** - Modern OpenAPI/Swagger documentation and testing UI
- **Docker** - Containerization

## Prerequisites

- .NET 9.0 SDK
- PostgreSQL database (running via Docker Compose)
- Docker (optional, for containerized deployment)

## Quick Start

### 1. Database Setup

Ensure the PostgreSQL database is running:

```bash
# From the parent directory (cursor-test)
docker-compose up -d
```

### 2. API Development

```bash
# Navigate to the API directory
cd ProcurementAPI

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API
dotnet run
```

The API will be available at:
- **API**: http://localhost:5001
- **NSwag UI**: http://localhost:5001/swagger/index.html

### 3. Docker Deployment

**Important**: On macOS, port 5000 is used by AirPlay. Use port 5001 instead.

```bash
# Build the Docker image
docker build -t procurement-api .

# Run the container (using port 5001 to avoid AirPlay conflict)
docker run -p 5001:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=myapp;Username=postgres;Password=postgres_password" \
  procurement-api
```

**Access the API:**
- **NSwag UI**: http://localhost:5001/swagger/index.html
- **API Base URL**: http://localhost:5001/api

## API Endpoints

### Suppliers

- `GET /api/suppliers` - Get all suppliers with pagination and filtering
- `GET /api/suppliers/{id}` - Get supplier by ID
- `GET /api/suppliers/performance` - Get supplier performance metrics
- `GET /api/suppliers/countries` - Get list of countries for filtering

### RFQs (Request for Quotes)

- `GET /api/rfqs` - Get all RFQs with pagination and filtering
- `GET /api/rfqs/{id}` - Get RFQ by ID with full details
- `GET /api/rfqs/statuses` - Get available RFQ statuses
- `GET /api/rfqs/summary` - Get RFQ summary statistics

### Items

- `GET /api/items` - Get all items with pagination and filtering
- `GET /api/items/{id}` - Get item by ID
- `GET /api/items/categories` - Get available item categories

### Quotes

- `GET /api/quotes` - Get all quotes with pagination and filtering
- `GET /api/quotes/{id}` - Get quote by ID
- `GET /api/quotes/by-rfq/{rfqId}` - Get quotes for specific RFQ

### Purchase Orders

- `GET /api/purchaseorders` - Get all purchase orders with pagination
- `GET /api/purchaseorders/{id}` - Get purchase order by ID
- `GET /api/purchaseorders/by-supplier/{supplierId}` - Get POs by supplier

## Testing the API

### Using NSwag UI (Recommended)

1. Open your browser to: http://localhost:5001/swagger/index.html
2. Browse available endpoints
3. Click "Try it out" on any endpoint
4. Modify parameters as needed
5. Click "Execute" to test the API

### Example API Calls

```bash
# Get suppliers with pagination
curl "http://localhost:5001/api/suppliers?page=1&pageSize=10"

# Get RFQs with filtering
curl "http://localhost:5001/api/rfqs?status=published&search=electronics"

# Get supplier performance
curl "http://localhost:5001/api/suppliers/performance?top=10"

# Get specific supplier
curl "http://localhost:5001/api/suppliers/1"
```

## Database Schema

The API connects to a PostgreSQL database with the following main entities:

- **Suppliers** - Company information and contact details (1000 records)
- **Items** - Products and services that can be quoted (1000 records)
- **RequestForQuotes** - RFQ documents with line items (1000 records)
- **RfqLineItems** - Individual items within an RFQ (10,000 records)
- **RfqSuppliers** - Supplier invitations to RFQs
- **Quotes** - Supplier responses to RFQ line items (10,000 records)
- **PurchaseOrders** - Orders created from awarded quotes (1000 records)
- **PurchaseOrderLines** - Individual items within a PO (10,000 records)

## Configuration

### Connection String

Update the connection string in `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=postgres_password"
  }
}
```

### Docker Environment Variables

When running in Docker, use:

```bash
ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=myapp;Username=postgres;Password=postgres_password"
```

### CORS Configuration

The API is configured to allow requests from Angular development servers:

```csharp
policy.WithOrigins("http://localhost:4200", "https://localhost:4200")
```

## Development

### Project Structure

```
ProcurementAPI/
├── Controllers/          # API controllers
├── Data/                # Entity Framework DbContext
├── DTOs/                # Data Transfer Objects
├── Models/              # Entity models
├── Program.cs           # Application entry point
├── appsettings.json     # Configuration
├── Dockerfile           # Docker configuration
└── README.md           # This file
```

### Adding New Endpoints

1. Create a new controller in the `Controllers/` directory
2. Define DTOs in the `DTOs/` directory
3. Use Entity Framework for data access
4. Add proper error handling and logging

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName

# Apply migrations to database
dotnet ef database update
```

## Docker Support

### Build Image

```bash
docker build -t procurement-api .
```

### Run Container

```bash
docker run -p 5001:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=myapp;Username=postgres;Password=postgres_password" \
  procurement-api
```

### Docker Compose Integration

Add to your `docker-compose.yml`:

```yaml
services:
  api:
    build: ./ProcurementAPI
    ports:
      - "5001:8080"  # Use 5001 on macOS
    environment:
      - ConnectionStrings__DefaultConnection=Host=postgres;Port=5432;Database=myapp;Username=postgres;Password=postgres_password
      - ASPNETCORE_ENVIRONMENT=Development
    depends_on:
      - postgres
```

## Troubleshooting

### Port Conflicts on macOS

If you get a "403 Forbidden" error on port 5000, it's likely due to AirPlay. Use port 5001 instead:

```bash
docker run -p 5001:8080 procurement-api
```

### Database Connection Issues

1. Ensure PostgreSQL is running: `docker-compose ps`
2. Check connection string format
3. Verify database exists: `docker-compose exec postgres psql -U postgres -d myapp`

### Container Won't Start

1. Check logs: `docker logs <container-id>`
2. Verify environment variables
3. Ensure database is accessible from container

## Angular Frontend Integration

The API is designed to work seamlessly with Angular frontends:

- CORS is configured for Angular development servers
- All endpoints return JSON data
- Pagination is supported for large datasets
- Filtering and sorting are available
- Error responses are standardized

## Performance Considerations

- Entity Framework queries are optimized with proper includes
- Pagination is implemented for all list endpoints
- Database indexes are configured for common queries
- Connection pooling is enabled by default

## Security

- Input validation on all endpoints
- SQL injection protection via Entity Framework
- CORS is configured for specific origins
- Non-root user in Docker containers

## Monitoring and Logging

- Structured logging with ILogger
- Error tracking and exception handling
- Performance monitoring via EF Core logging
- Health check endpoints for container orchestration

## Deployment

### Production Considerations

1. Use environment-specific connection strings
2. Configure proper CORS origins
3. Enable HTTPS in production
4. Set up proper logging and monitoring
5. Use container orchestration (Kubernetes, Docker Swarm)

### Environment Variables

```bash
ConnectionStrings__DefaultConnection=Host=prod-db;Port=5432;Database=myapp;Username=prod_user;Password=secure_password
ASPNETCORE_ENVIRONMENT=Production
```

## Contributing

1. Follow .NET coding conventions
2. Add proper XML documentation to public APIs
3. Include error handling and logging
4. Test endpoints thoroughly
5. Update this README for new features

## License

This project is licensed under the MIT License.