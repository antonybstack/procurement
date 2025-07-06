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

### Quotes

- `GET /api/quotes` - Get all quotes with pagination and filtering
- `GET /api/quotes/{id}` - Get quote by ID with full details
- `POST /api/quotes` - Create a new quote
- `PUT /api/quotes/{id}` - Update an existing quote
- `DELETE /api/quotes/{id}` - Delete a quote
- `GET /api/quotes/statuses` - Get available quote statuses
- `GET /api/quotes/rfq/{rfqId}` - Get quotes by RFQ ID
- `GET /api/quotes/supplier/{supplierId}` - Get quotes by supplier ID with pagination
- `GET /api/quotes/summary` - Get quote summary statistics

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

# Get quotes with filtering
curl "http://localhost:5001/api/quotes?status=submitted&search=electronics"

# Get quotes by RFQ
curl "http://localhost:5001/api/quotes/rfq/1"

# Get quotes by supplier
curl "http://localhost:5001/api/quotes/supplier/1?page=1&pageSize=10"

# Get quote summary statistics
curl "http://localhost:5001/api/quotes/summary"

# Create a new quote
curl -X POST "http://localhost:5001/api/quotes" \
  -H "Content-Type: application/json" \
  -d '{
    "rfqId": 1,
    "supplierId": 1,
    "lineItemId": 1,
    "quoteNumber": "Q-2024-001",
    "unitPrice": 100.00,
    "totalPrice": 1000.00,
    "quantityOffered": 10,
    "deliveryDate": "2024-12-31",
    "paymentTerms": "Net 30",
    "warrantyPeriodMonths": 12
  }'
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

## Quotes Management

The Quotes API provides comprehensive functionality for managing supplier quotes in the procurement process.

### Quote Lifecycle

1. **Creation** - Suppliers submit quotes in response to RFQ line items
2. **Review** - Procurement team reviews and evaluates quotes
3. **Award** - Best quotes are selected and awarded
4. **Purchase Order** - Awarded quotes are converted to purchase orders

### Quote Statuses

- **Pending** - Quote submitted, awaiting review
- **Submitted** - Quote under review
- **Awarded** - Quote selected for purchase order
- **Rejected** - Quote not selected
- **Expired** - Quote validity period expired

### Key Features

- **Comprehensive Filtering** - Filter by status, RFQ, supplier, date range
- **Pagination** - Handle large datasets efficiently
- **Full CRUD Operations** - Create, read, update, delete quotes
- **Relationship Navigation** - Access related RFQ, supplier, and item data
- **Summary Statistics** - Get overview of quote performance
- **Validation** - Ensure data integrity and business rules

### Quote Data Model

Each quote includes:
- **Basic Information**: Quote number, status, submission date
- **Pricing**: Unit price, total price, quantity offered
- **Delivery**: Delivery date, payment terms, warranty
- **Technical Details**: Compliance notes, validity period
- **Relationships**: Links to RFQ, supplier, and line item

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

### Data Transfer Objects (DTOs)

The API uses DTOs to control data exposure and provide optimized data structures:

#### Quote DTOs

- **QuoteDto** - Full quote details with related entities
- **QuoteSummaryDto** - Simplified quote information for lists
- **QuoteCreateDto** - Data required to create a new quote
- **QuoteUpdateDto** - Data that can be updated on an existing quote

#### Common DTOs

- **PaginatedResult<T>** - Standard pagination wrapper for list responses
- **SupplierDto** - Supplier information with all fields
- **RfqDto** - RFQ information with summary data
- **ItemDto** - Item/product information

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

## Error Handling and Validation

### Quote Validation Rules

- **Quote Number** - Required, unique within RFQ-supplier-line item combination
- **Unit Price** - Required, must be positive
- **Total Price** - Required, must be positive and match unit price × quantity
- **Quantity Offered** - Required, must be positive
- **Status** - Must be a valid QuoteStatus enum value
- **RFQ, Supplier, Line Item** - Must reference existing entities

### Error Responses

The API returns standardized error responses:

```json
{
  "message": "Error description",
  "statusCode": 400,
  "details": "Additional error information"
}
```

### Common Error Codes

- **400 Bad Request** - Invalid input data or validation errors
- **404 Not Found** - Resource not found
- **409 Conflict** - Concurrency conflict or duplicate resource
- **500 Internal Server Error** - Unexpected server error

### Business Logic Validation

- Prevents duplicate quotes for same supplier-line item combination
- Validates relationships between RFQ, supplier, and line items
- Ensures quote status transitions are valid
- Maintains referential integrity across related entities

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