# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Rules

- Whenever `sudo` is required, ask for input of the user's password, then re-run the failed command with `echo "password" | sudo -S <command>`, replacing `password` with the user's input.
- Do not back up database data or maintain migration scripts because all data is sample demo data and subject to change.
- Do not back up files when making large refactorings as I am handling that through git version control.

## Project Overview

This is a comprehensive Procurement Management System with AI-powered semantic search capabilities. The system consists of a .NET 9 API backend, Angular 20 frontend, PostgreSQL database, and AI/ML components for document ingestion and semantic search.

### Technology Stack

- **Backend**: .NET 9 Web API with Entity Framework Core
- **Frontend**: Angular 20 with standalone components, Tailwind CSS, AG Grid
- **Database**: PostgreSQL 15 with pgvector extension for vector embeddings
- **AI/ML**: Microsoft Semantic Kernel, OpenAI GPT models, vector embeddings
- **Infrastructure**: Docker Compose, nginx reverse proxy, Ollama for local LLM
- **Observability**: OpenTelemetry, Serilog, Grafana, Prometheus, Loki

## Development Environment Setup

### macOS Development (Primary)

```bash
# Start full stack with nginx on standard ports 80
./mac-deploy.sh

# Individual service management
./start.sh           # Start all services in sequence
./start-db.sh        # PostgreSQL database only
./start-api.sh       # .NET API only
./start-frontend.sh  # Angular frontend only
./stop.sh           # Stop all services
```

### Linux/Digital Ocean Deployment

```bash
# Full production deployment with Let's Encrypt SSL
./minimal-deploy.sh

# HTTPS-enabled deployment
./deploy-https.sh
```

## Common Development Commands

### Backend (.NET API)

```bash
# API development (from ProcurementAPI/)
dotnet restore
dotnet build
dotnet run                    # Runs on http://localhost:5001

# Database operations
dotnet ef migrations add MigrationName
dotnet ef database update
./reset-db.sh                # WARNING: Deletes all data

# Testing - ALWAYS run after C# changes per .cursor/rules
cd tests/ProcurementAPI.Tests
dotnet test                   # Run all integration tests
dotnet test --verbosity normal
dotnet test --filter "FullyQualifiedName~SupplierControllerTests"
./run-tests.sh --coverage    # Run with code coverage
```

### Frontend (Angular)

```bash
# Frontend development (from app/)
npm install
npm start                     # Runs on http://localhost:4200
npm run build
npm test

# Follow Angular signals over RxJS per .cursor/rules
# Never use zone.js, prefer standalone components
```

### Docker Services

```bash
# Individual service management
docker-compose -f docker-compose.db.yml up -d        # Database
docker-compose -f docker-compose.api.yml up -d       # API
docker-compose -f docker-compose.frontend.yml up -d  # Frontend
docker-compose -f docker-compose.elastic.yml up -d   # Elasticsearch
docker-compose -f docker-compose.grafana.yml up -d   # Observability

# Health checks and status
./health-check.sh
./status.sh
docker-compose ps
```

## Architecture Overview

### Multi-Layered Architecture

The system follows a clean architecture pattern with clear separation of concerns:

**API Layer** (`ProcurementAPI/Controllers/`):

- RESTful controllers for Suppliers, RFQs, Quotes, Items, Documents
- AI-powered chat and search endpoints
- Health checks and observability endpoints

**Service Layer** (`ProcurementAPI/Services/`):

- `SupplierService` - Business logic for supplier management
- `VectorStoreService` - Vector embeddings and semantic search
- `AiVectorizationService` - Document ingestion and AI processing
- `DataServices/` - Data access layer abstractions

**Data Layer** (`ProcurementAPI/Data/`):

- Entity Framework DbContext with PostgreSQL
- Vector store integration for AI embeddings
- Database models and migrations

### AI/ML Integration

The system implements sophisticated AI capabilities:

**Vector Store Implementation**:

- PostgreSQL with pgvector extension for vector embeddings
- Microsoft Semantic Kernel for vector operations
- Support for multiple entity types (suppliers, items, RFQs, quotes)
- Real-time vector updates and similarity search

**Document Processing**:

- PDF ingestion with PdfPig library
- Automatic text extraction and chunking
- Vector embedding generation for semantic search
- Document storage in `ProcurementAPI/Documents/`

**AI Services Configuration**:

- OpenAI GPT models for chat and analysis
- Ollama for local LLM deployment
- Configurable model selection in `Program.cs`

### Frontend Architecture

**Feature-Based Organization**:

```
app/src/app/features/
├── suppliers/    # Supplier management module
├── rfqs/        # Request for Quote module
├── quotes/      # Quote management module
├── items/       # Item/product module
└── search/      # AI-powered search and chat
```

**Key Patterns**:

- Standalone components (no NgModules)
- Angular signals for state management (preferred over RxJS)
- Lazy-loaded feature modules
- AG Grid for complex data tables
- Tailwind CSS for styling

## Database Schema & Business Logic

### Core Entities

- **Suppliers** - Company profiles with performance metrics
- **Items** - Products/services catalog with specifications
- **RequestForQuotes (RFQs)** - Procurement requests with line items
- **Quotes** - Supplier responses to RFQ line items
- **PurchaseOrders** - Orders created from awarded quotes

### Relationships

- RFQs contain multiple RfqLineItems
- RfqSuppliers represents invited suppliers
- Quotes link suppliers to specific RFQ line items
- PurchaseOrders convert awarded quotes to orders

### Vector Store Models

- Separate vector models for each entity type
- Optimized for semantic search and similarity matching
- Real-time synchronization with EF Core entities

## Testing Strategy

### Backend Testing

- Comprehensive integration tests using WebApplicationFactory
- In-memory database for fast, isolated testing
- Full CRUD operation coverage for all endpoints
- Error handling and edge case testing
- **Critical**: Always run `dotnet test` after C# changes

### Frontend Testing

- Vitest for unit testing (configured in `vitest.config.ts`)
- Karma/Jasmine for integration testing
- Component testing with Angular Testing Library patterns

## AI and Search Features

### Semantic Search

The system provides advanced semantic search capabilities:

- Vector similarity search across all entity types
- Chat interface for natural language queries
- Document-based question answering
- Real-time search suggestions

### Chat Interface

- GPT-powered conversational AI
- Context-aware responses using vector store
- Integration with procurement data
- Accessible via `/search` route in frontend

## Deployment and Infrastructure

### Development (macOS with OrbStack)

- Use `mac-deploy.sh` for local development
- nginx reverse proxy with SSL certificates via mkcert
- Docker services managed individually or via `start.sh`

### Production (Linux/Digital Ocean)

- Use `minimal-deploy.sh` for production deployment
- Let's Encrypt SSL certificates
- UFW firewall configuration
- Optimized PostgreSQL settings

### Observability Stack

- **OpenTelemetry**: Distributed tracing and metrics
- **Serilog**: Structured logging with multiple sinks
- **Grafana**: Dashboards and visualization
- **Prometheus**: Metrics collection
- **Loki**: Log aggregation

## Configuration Management

### API Configuration

- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Local development
- `appsettings.Docker.json` - Container deployment
- User secrets for sensitive data (OpenAI keys, etc.)

### Environment Variables

```bash
OpenAIKey
ModelName
EmbeddingModelName
```

## Important Development Notes

### Cursor Rules Integration

The project includes specific Cursor rules that should be followed:

- Always run `dotnet test` after .NET changes
- Prefer Angular signals over RxJS observables
- Never use zone.js in Angular components
- Run frontend tests after Angular changes

### Security Considerations

- PostgreSQL configured with performance-optimized settings
- CORS configured for development (localhost:4200)
- SSL/TLS encryption for production deployments
- No hardcoded secrets (use User Secrets or environment variables)

### Performance Optimizations

- EF Core query optimization with proper includes
- Database connection pooling
- Vector store indexing for fast similarity search
- Lazy loading for Angular feature modules
- AG Grid virtual scrolling for large datasets

This architecture supports a full-featured procurement system with modern AI capabilities, comprehensive testing, and production-ready deployment options.
