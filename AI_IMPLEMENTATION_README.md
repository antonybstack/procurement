# AI-Powered Supplier Recommendations

This document describes the AI implementation for intelligent supplier recommendations in the procurement application.

## Architecture Overview

The AI implementation uses a **traditional API-based approach** with SQLCoder for intelligent SQL generation, providing lightweight and fast supplier recommendations.

```
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │   .NET API      │    │   AI Service    │
│   (Angular)     │───▶│   (Controller)  │───▶│   (SQLCoder)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
                                │                        │
                                ▼                        ▼
                       ┌─────────────────┐    ┌─────────────────┐
                       │   PostgreSQL    │    │   Database      │
                       │   Database      │    │   Schema        │
                       └─────────────────┘    └─────────────────┘
```

## Why Traditional API over MCP?

1. **Performance**: Direct API calls are faster than MCP protocol overhead
2. **Lightweight**: Lower memory footprint on Mac/OrbStack
3. **Real-time**: Quick responses for procurement decisions
4. **Integration**: Seamless with existing Angular/.NET architecture
5. **Simplicity**: Easier to debug and maintain

## Technology Stack

### AI Model: SQLCoder
- **SQLCoder-7B** or **SQLCoder-3B** for lightweight deployment
- Excellent at understanding database schemas
- Generates intelligent SQL queries based on natural language
- Can be containerized easily for OrbStack

### Components
- **AI Service**: Python Flask service with SQLCoder integration
- **.NET API**: Controller layer for AI recommendations
- **Database**: PostgreSQL with procurement schema
- **Frontend**: Angular integration (ready for implementation)

## Quick Start

### 1. Start the AI Service

```bash
# Start the AI recommendation service
./start-ai.sh
```

### 2. Test the Service

```bash
# Check if the service is healthy
curl http://localhost:8000/health

# Test SQL generation
curl -X POST http://localhost:8000/generate \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Find suppliers for electronic components with good ratings"
  }'
```

### 3. Use the .NET API

```bash
# Get supplier recommendations for an item
curl "http://localhost:5001/api/airecommendations/suppliers/ITEM001?quantity=100&maxResults=5"

# Get recommendations by description
curl "http://localhost:5001/api/airecommendations/suppliers/by-description?description=electronic%20component&category=electronics"

# Get performance analysis
curl "http://localhost:5001/api/airecommendations/analysis/ITEM001"
```

## API Endpoints

### AI Service (Port 8000)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/health` | GET | Service health check |
| `/generate` | POST | Generate SQL queries |
| `/schema` | GET | Get database schema |
| `/models` | GET | List available models |

### .NET API (Port 5001)

| Endpoint | Method | Description |
|----------|--------|-------------|
| `/api/airecommendations/suppliers/{itemCode}` | GET | Get supplier recommendations by item code |
| `/api/airecommendations/suppliers/by-description` | GET | Get recommendations by description |
| `/api/airecommendations/analysis/{itemCode}` | GET | Get supplier performance analysis |
| `/api/airecommendations/health` | GET | AI service health check |

## Features

### 1. Intelligent Supplier Recommendations

The AI analyzes:
- **Historical Performance**: Quote success rates, awarded contracts
- **Supplier Ratings**: Quality and reliability scores
- **Price Competitiveness**: Average pricing and market position
- **Geographic Preferences**: Country-based filtering
- **Category Expertise**: Supplier specialization in item categories

### 2. Smart SQL Generation

SQLCoder generates queries that:
- Join multiple tables intelligently
- Apply business logic filters
- Calculate performance metrics
- Order results by relevance
- Include reasoning for recommendations

### 3. Performance Analysis

Provides insights on:
- Market competitiveness
- Price variance analysis
- Supplier performance trends
- Recommendations for procurement strategy

## Configuration

### Environment Variables

```bash
# AI Service Configuration
MODEL_NAME=defog/sqlcoder-7b
MAX_TOKENS=512
TEMPERATURE=0.1
TOP_P=0.95
DATABASE_URL=postgresql://postgres:postgres_password@postgres:5432/myapp
```

### .NET API Configuration

```json
{
  "AiService": {
    "BaseUrl": "http://ai-recommendation-service:8000"
  }
}
```

## Database Schema

The AI service uses these key tables and views:

### Core Tables
- `suppliers` - Supplier information and ratings
- `items` - Product/part information
- `quotes` - Supplier quotes and pricing
- `rfq_line_items` - RFQ line items
- `purchase_orders` - Awarded contracts

### Performance Views
- `supplier_performance` - Aggregated supplier metrics
- `item_supplier_history` - Item-supplier relationship history

## Example Queries

### Supplier Recommendations Query

```sql
SELECT 
    s.supplier_id,
    s.supplier_code,
    s.company_name,
    s.country,
    s.rating,
    COALESCE(AVG(q.unit_price), 0) as avg_price,
    COUNT(DISTINCT q.quote_id) as quote_count,
    COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) as awarded_count,
    CASE 
        WHEN COUNT(DISTINCT q.quote_id) > 0 
        THEN ROUND((COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) * 100.0 / COUNT(DISTINCT q.quote_id)), 2)
        ELSE 0 
    END as success_rate,
    CASE 
        WHEN s.rating >= 4 THEN 'High rating supplier with excellent track record'
        WHEN s.rating >= 3 THEN 'Good supplier with solid performance'
        ELSE 'Supplier with room for improvement'
    END as reasoning
FROM suppliers s
LEFT JOIN quotes q ON s.supplier_id = q.supplier_id
LEFT JOIN rfq_line_items rli ON q.line_item_id = rli.line_item_id
LEFT JOIN items i ON rli.item_id = i.item_id
WHERE s.is_active = true
AND (i.item_code = 'ITEM001' OR i.category = 'electronics')
GROUP BY s.supplier_id, s.supplier_code, s.company_name, s.country, s.rating
HAVING COUNT(DISTINCT q.quote_id) > 0
ORDER BY s.rating DESC, success_rate DESC, avg_price ASC
LIMIT 10;
```

## Integration with Frontend

### Angular Service Example

```typescript
@Injectable({
  providedIn: 'root'
})
export class AiRecommendationService {
  constructor(private apiService: ApiService) { }

  getSupplierRecommendations(itemCode: string, quantity: number = 1): Observable<SupplierRecommendationDto[]> {
    return this.apiService.get<SupplierRecommendationDto[]>(`airecommendations/suppliers/${itemCode}`, { quantity });
  }

  getRecommendationsByDescription(description: string, category?: string): Observable<SupplierRecommendationDto[]> {
    return this.apiService.get<SupplierRecommendationDto[]>('airecommendations/suppliers/by-description', { 
      description, 
      category 
    });
  }

  getPerformanceAnalysis(itemCode: string): Observable<SupplierPerformanceAnalysisDto> {
    return this.apiService.get<SupplierPerformanceAnalysisDto>(`airecommendations/analysis/${itemCode}`);
  }
}
```

## Deployment

### Local Development (OrbStack)

```bash
# Start all services
./start-db.sh
./start-api.sh
./start-ai.sh

# Or start everything at once
./start.sh
```

### Production Considerations

1. **Model Selection**: Use SQLCoder-3B for faster inference
2. **Caching**: Implement Redis for query result caching
3. **Load Balancing**: Multiple AI service instances
4. **Monitoring**: Add metrics and alerting
5. **Security**: Implement API authentication

## Troubleshooting

### Common Issues

1. **AI Service Not Starting**
   ```bash
   # Check logs
   docker-compose -f docker-compose.ai.yml logs ai-recommendation-service
   
   # Check network
   docker network ls | grep postgres_network
   ```

2. **Database Connection Issues**
   ```bash
   # Verify database is running
   docker-compose ps postgres
   
   # Test connection
   docker-compose exec postgres psql -U postgres -d myapp -c "SELECT 1;"
   ```

3. **API Connection Issues**
   ```bash
   # Test AI service health
   curl http://localhost:8000/health
   
   # Check .NET API logs
   docker-compose logs procurement-api
   ```

## Performance Optimization

### For Production Use

1. **Model Optimization**
   - Use quantized models (4-bit or 8-bit)
   - Implement model caching
   - Use smaller models for faster inference

2. **Query Optimization**
   - Cache frequently used queries
   - Implement query result caching
   - Use database indexes effectively

3. **Infrastructure**
   - Use GPU acceleration if available
   - Implement horizontal scaling
   - Add monitoring and alerting

## Future Enhancements

1. **Advanced Features**
   - Multi-language support
   - Custom model fine-tuning
   - Real-time learning from user feedback

2. **Integration**
   - ERP system integration
   - External supplier databases
   - Market data feeds

3. **Analytics**
   - Recommendation accuracy tracking
   - User behavior analysis
   - Performance optimization insights

## Security Considerations

1. **API Security**
   - Implement authentication/authorization
   - Rate limiting
   - Input validation

2. **Data Privacy**
   - Encrypt sensitive data
   - Implement data retention policies
   - Audit logging

3. **Model Security**
   - Validate generated SQL queries
   - Implement query execution limits
   - Monitor for SQL injection attempts

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review service logs
3. Test individual components
4. Verify network connectivity

The AI implementation provides a solid foundation for intelligent supplier recommendations while maintaining simplicity and performance for your procurement application. 