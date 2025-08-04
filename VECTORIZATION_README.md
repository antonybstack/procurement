# AI Vectorization Setup for Procurement System

This document describes the AI vectorization infrastructure that enables semantic search and supplier recommendations in the procurement system.

## 🏗️ Architecture Overview

The AI stack uses a **Retrieval-Augmented Generation (RAG)** pattern with the following components:

- **PostgreSQL with pgvector**: Stores both structured data and vector embeddings
- **Ollama**: Local LLM service for generating embeddings and text completions
- **C#/.NET API**: Orchestrates the entire AI workflow
- **Llama 3.2 3B**: Efficient local model for embedding generation

## 🚀 Quick Start

### 1. Automated Setup

Run the comprehensive setup script:

```bash
./setup-vectorization.sh
```

This script will:
- Start the database with vector support
- Start Ollama and pull the required model
- Build and start the API
- Vectorize existing data
- Test the system

### 2. Manual Setup

If you prefer manual control:

```bash
# Start database
./start-db.sh

# Start Ollama
./start-ollama.sh

# Start API
./start-api.sh

# Vectorize data (after API is ready)
curl -X POST http://localhost:5000/api/ai/vectorize/suppliers
curl -X POST http://localhost:5000/api/ai/vectorize/items
```

## 📊 Database Schema

The following tables now include vector embeddings:

- `suppliers.embedding` - Vector representation of supplier data
- `items.embedding` - Vector representation of item data
- `request_for_quotes.embedding` - Vector representation of RFQ data
- `rfq_line_items.embedding` - Vector representation of line item data
- `quotes.embedding` - Vector representation of quote data
- `purchase_orders.embedding` - Vector representation of PO data
- `purchase_order_lines.embedding` - Vector representation of PO line data

## 🔍 Available AI Endpoints

### Vectorization
- `POST /api/ai/vectorize/suppliers` - Generate embeddings for all suppliers
- `POST /api/ai/vectorize/items` - Generate embeddings for all items

### Search
- `GET /api/ai/search/suppliers?query={query}&limit={limit}` - Find similar suppliers
- `GET /api/ai/search/items?query={query}&limit={limit}` - Find similar items
- `GET /api/ai/search/semantic?query={query}&limit={limit}` - Search across all entities

### Recommendations
- `GET /api/ai/suggest/suppliers?requirement={requirement}&limit={limit}` - Suggest suppliers for requirements

### Health Check
- `GET /api/ai/status` - Check vectorization service status

## 🧪 Example Queries

### Supplier Search
```bash
# Find aluminum suppliers
curl "http://localhost:5000/api/ai/search/suppliers?query=aluminum+supplier&limit=5"

# Find AS9100 certified suppliers
curl "http://localhost:5000/api/ai/search/suppliers?query=AS9100+certified+supplier&limit=10"
```

### Item Search
```bash
# Find electronic components
curl "http://localhost:5000/api/ai/search/items?query=electronic+components&limit=5"

# Find specific materials
curl "http://localhost:5000/api/ai/search/items?query=aluminum+7075+bracket&limit=10"
```

### Semantic Search
```bash
# Search across all entities
curl "http://localhost:5000/api/ai/search/semantic?query=high+quality+suppliers&limit=20"
```

### Supplier Recommendations
```bash
# Get supplier suggestions for specific requirements
curl "http://localhost:5000/api/ai/suggest/suppliers?requirement=AS9100+certified+supplier+for+aluminum+parts&limit=5"
```

## 🔧 Configuration

### Ollama Configuration

The system uses Ollama with the following configuration:

- **Model**: `llama3.2:3b` (efficient for local deployment)
- **Embedding Dimension**: 1536
- **URL**: `http://localhost:11434` (default)

You can modify the model in `ProcurementAPI/Services/AiVectorizationService.cs`:

```csharp
private const string ModelName = "llama3.2:3b"; // Change this to use different models
```

### Database Configuration

Vector embeddings are stored as `vector(1536)` columns in PostgreSQL with pgvector extension.

## 📈 Performance Considerations

### Vector Indexes

The system uses IVFFlat indexes for efficient similarity search:

```sql
CREATE INDEX idx_suppliers_embedding ON suppliers USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
```

### Batch Processing

For large datasets, consider:

1. **Batch vectorization**: Process entities in smaller batches
2. **Background processing**: Use background services for vectorization
3. **Incremental updates**: Only re-vectorize changed entities

## 🐛 Troubleshooting

### Common Issues

1. **Ollama not responding**
   ```bash
   # Check if Ollama is running
   curl http://localhost:11434/api/tags
   
   # Restart Ollama (preserves all models and data)
   ./restart-ollama.sh
   ```

2. **Model not available**
   ```bash
   # Pull the model manually
   curl -X POST http://localhost:11434/api/pull -d '{"name": "llama3.2:3b"}'
   ```

3. **Database connection issues**
   ```bash
   # Check database status
   docker exec procurement-postgres pg_isready -U postgres
   ```

4. **Vectorization failing**
   ```bash
   # Check API logs
   docker logs procurement-api
   
   # Test embedding generation
   curl http://localhost:5000/api/ai/status
   ```

### Logs

- **API Logs**: `docker logs procurement-api`
- **Ollama Logs**: `docker logs procurement-ollama`
- **Database Logs**: `docker logs procurement-postgres`

## 🔄 Updating Vectorizations

### Re-vectorize All Data
```bash
curl -X POST http://localhost:5000/api/ai/vectorize/suppliers
curl -X POST http://localhost:5000/api/ai/vectorize/items
```

### Re-vectorize Specific Entity
The system automatically generates embeddings when entities are created/updated through the API.

## 🎯 Integration with Frontend

### Angular Integration Example

```typescript
// Search suppliers
searchSuppliers(query: string): Observable<Supplier[]> {
  return this.http.get<{suppliers: Supplier[]}>(`${this.apiUrl}/ai/search/suppliers?query=${encodeURIComponent(query)}`)
    .pipe(map(response => response.suppliers));
}

// Get supplier recommendations
getSupplierRecommendations(requirement: string): Observable<Supplier[]> {
  return this.http.get<{suppliers: Supplier[]}>(`${this.apiUrl}/ai/suggest/suppliers?requirement=${encodeURIComponent(requirement)}`)
    .pipe(map(response => response.suppliers));
}
```

## 📚 Next Steps

1. **Test the AI endpoints** with your existing data
2. **Integrate AI search** into your Angular frontend
3. **Monitor performance** and adjust vectorization as needed
4. **Add more sophisticated prompts** for better recommendations
5. **Implement caching** for frequently searched queries
6. **Add user feedback** to improve search relevance

## 🔗 Related Documentation

- [Implementation Plan Phase 2](implementation-plan-phase-2.md)
- [Database Schema](database-schema.sql)
- [API Documentation](http://localhost:5000/swagger)
- [Ollama Documentation](https://ollama.ai/docs)
- [pgvector Documentation](https://github.com/pgvector/pgvector) 