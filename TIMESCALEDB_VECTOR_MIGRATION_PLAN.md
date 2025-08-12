# TimescaleDB Vector Migration Plan

## 🎯 **Objective**
Migrate from basic PostgreSQL + pgvector to TimescaleDB with pgvectorscale and pgai to enable advanced supplier intelligence and semantic search capabilities for the procurement platform.

## 📋 **Current State**
- **Database**: pgvector/pgvector:0.8.0-pg17-bookworm
- **Extensions**: pgvector for basic vector operations
- **Management**: pgAdmin web interface
- **Data**: Structured supplier data with basic vector embeddings for document search

## 🚀 **Target State**
- **Database**: timescale/timescaledb:latest-pg17
- **Extensions**: pgvectorscale + pgai for advanced vector operations
- **Management**: Direct database access (removing pgAdmin overhead)
- **Data**: Enhanced supplier intelligence with semantic search and AI-driven matching

---

## 📊 **Phase 1: Infrastructure Migration**

### **1.1 Database Container Upgrade**

**Replace docker-compose.db.yml:**
```yaml
services:
  timescaledb:
    image: timescale/timescaledb:latest-pg17
    container_name: timescale_db
    restart: unless-stopped
    environment:
      POSTGRES_DB: procurement_ai
      POSTGRES_USER: procurement_user
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
      POSTGRES_INITDB_ARGS: "--encoding=UTF-8 --lc-collate=C --lc-ctype=C"
      # TimescaleDB specific
      TIMESCALEDB_TELEMETRY: 'off'
    ports:
      - "5432:5432"
    volumes:
      - timescale_data:/var/lib/postgresql/data
      - ./database-schema-timescale.sql:/docker-entrypoint-initdb.d/01-schema.sql
      - ./vectorscale-setup.sql:/docker-entrypoint-initdb.d/02-vectorscale.sql
      - ./pgai-setup.sql:/docker-entrypoint-initdb.d/03-pgai.sql
      - ./postgresql-timescale.conf:/etc/postgresql/postgresql.conf
    command: >
      postgres 
      -c config_file=/etc/postgresql/postgresql.conf
      -c shared_preload_libraries='timescaledb,vectorscale'
      -c max_connections=200 
      -c shared_buffers=512MB 
      -c effective_cache_size=2GB
      -c maintenance_work_mem=128MB
      -c work_mem=8MB
      -c timescaledb.max_background_workers=8
    healthcheck:
      test: [ "CMD-SHELL", "pg_isready -U procurement_user -d procurement_ai" ]
      interval: 30s
      timeout: 10s
      retries: 5
      start_period: 60s
    networks:
      - timescale_network
    deploy:
      resources:
        limits:
          memory: 2G
          cpus: '2.0'
        reservations:
          memory: 1G
          cpus: '1.0'

volumes:
  timescale_data:
    driver: local

networks:
  timescale_network:
    driver: bridge
```

### **1.2 Extension Setup Scripts**

**Create vectorscale-setup.sql:**
```sql
-- Enable TimescaleDB
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Enable pgvectorscale for high-performance vector operations
CREATE EXTENSION IF NOT EXISTS vectorscale CASCADE;

-- Verify installations
SELECT extname, extversion FROM pg_extension WHERE extname IN ('timescaledb', 'vector', 'vectorscale');
```

**Create pgai-setup.sql:**
```sql
-- Enable pgai for AI/ML capabilities
CREATE EXTENSION IF NOT EXISTS ai CASCADE;

-- Configure AI providers (OpenAI, etc.)
-- This will be configured via environment variables or application code
SELECT ai.list_providers();
```

---

## 📊 **Phase 2: Schema Migration & Vector Enhancement**

### **2.1 Enhanced Supplier Schema**

**Create database-schema-timescale.sql:**
```sql
-- Enable extensions first
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE EXTENSION IF NOT EXISTS vectorscale CASCADE;
CREATE EXTENSION IF NOT EXISTS ai CASCADE;

-- Enhanced Suppliers table
CREATE TABLE suppliers (
    supplier_id SERIAL PRIMARY KEY,
    supplier_code VARCHAR(20) UNIQUE NOT NULL,
    company_name VARCHAR(255) NOT NULL,
    contact_name VARCHAR(255),
    email VARCHAR(255),
    phone VARCHAR(50),
    address TEXT,
    city VARCHAR(100),
    state VARCHAR(100),
    country VARCHAR(100),
    postal_code VARCHAR(20),
    tax_id VARCHAR(50),
    payment_terms VARCHAR(100),
    credit_limit DECIMAL(15,2),
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    
    -- Enhanced fields for AI/ML
    industry_focus TEXT[],
    company_size VARCHAR(50),
    founded_year INTEGER,
    certifications TEXT[],
    specializations TEXT[],
    description TEXT,
    website VARCHAR(255)
);

-- Supplier capabilities (enhanced)
CREATE TABLE supplier_capabilities (
    capability_id SERIAL PRIMARY KEY,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    capability_type VARCHAR(100) NOT NULL,
    capability_value TEXT NOT NULL,
    proficiency_level VARCHAR(20) CHECK (proficiency_level IN ('Basic', 'Intermediate', 'Advanced', 'Expert')),
    years_experience INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Supplier vector embeddings table
CREATE TABLE supplier_embeddings (
    embedding_id SERIAL PRIMARY KEY,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    embedding_type VARCHAR(50) NOT NULL, -- 'profile', 'capabilities', 'description'
    source_text TEXT NOT NULL,
    embedding VECTOR(1536), -- OpenAI ada-002/text-embedding-3-small size
    model_name VARCHAR(100) NOT NULL DEFAULT 'text-embedding-3-small',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Create vectorscale index for high-performance similarity search
CREATE INDEX CONCURRENTLY supplier_embeddings_vector_idx 
ON supplier_embeddings 
USING vectorscale (embedding);

-- Supplier performance tracking (time-series optimized)
CREATE TABLE supplier_performance_metrics (
    metric_id SERIAL,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    metric_date DATE NOT NULL,
    quotes_submitted INTEGER DEFAULT 0,
    quotes_awarded INTEGER DEFAULT 0,
    total_value DECIMAL(15,2) DEFAULT 0,
    avg_response_time_hours INTEGER,
    quality_score DECIMAL(3,2),
    delivery_performance DECIMAL(3,2),
    created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Convert to TimescaleDB hypertable for time-series performance
SELECT create_hypertable('supplier_performance_metrics', 'metric_date', 
    chunk_time_interval => INTERVAL '1 month',
    if_not_exists => TRUE
);

-- Create indexes for optimal query performance
CREATE INDEX idx_suppliers_active ON suppliers(is_active) WHERE is_active = true;
CREATE INDEX idx_suppliers_country ON suppliers(country);
CREATE INDEX idx_suppliers_rating ON suppliers(rating);
CREATE INDEX idx_suppliers_industry ON suppliers USING GIN(industry_focus);
CREATE INDEX idx_suppliers_specializations ON suppliers USING GIN(specializations);
CREATE INDEX idx_suppliers_text_search ON suppliers USING GIN(to_tsvector('english', company_name || ' ' || COALESCE(description, '')));

CREATE INDEX idx_capabilities_type ON supplier_capabilities(capability_type);
CREATE INDEX idx_capabilities_supplier ON supplier_capabilities(supplier_id);

CREATE INDEX idx_embeddings_type ON supplier_embeddings(embedding_type);
CREATE INDEX idx_embeddings_supplier ON supplier_embeddings(supplier_id);
CREATE INDEX idx_embeddings_updated ON supplier_embeddings(updated_at);
```

### **2.2 Data Migration Functions**

```sql
-- Function to generate supplier profile embeddings using pgai
CREATE OR REPLACE FUNCTION generate_supplier_embedding(p_supplier_id INTEGER)
RETURNS VOID AS $$
DECLARE
    v_profile_text TEXT;
    v_embedding VECTOR(1536);
BEGIN
    -- Build comprehensive supplier profile text
    SELECT 
        company_name || ' ' ||
        COALESCE('Industry: ' || array_to_string(industry_focus, ', ') || '. ', '') ||
        COALESCE('Specializations: ' || array_to_string(specializations, ', ') || '. ', '') ||
        COALESCE('Description: ' || description || '. ', '') ||
        COALESCE('Location: ' || city || ', ' || country || '. ', '') ||
        COALESCE('Size: ' || company_size || '. ', '') ||
        COALESCE('Founded: ' || founded_year::text || '. ', '') ||
        COALESCE('Certifications: ' || array_to_string(certifications, ', ') || '. ', '')
    INTO v_profile_text
    FROM suppliers 
    WHERE supplier_id = p_supplier_id;

    -- Generate embedding using pgai
    SELECT ai.openai_embed('text-embedding-3-small', v_profile_text, _dimensions => 1536)
    INTO v_embedding;

    -- Upsert embedding
    INSERT INTO supplier_embeddings (supplier_id, embedding_type, source_text, embedding, model_name)
    VALUES (p_supplier_id, 'profile', v_profile_text, v_embedding, 'text-embedding-3-small')
    ON CONFLICT (supplier_id, embedding_type) 
    DO UPDATE SET 
        source_text = EXCLUDED.source_text,
        embedding = EXCLUDED.embedding,
        updated_at = NOW();
END;
$$ LANGUAGE plpgsql;

-- Function for semantic supplier search
CREATE OR REPLACE FUNCTION search_suppliers_semantic(
    p_query TEXT,
    p_limit INTEGER DEFAULT 10,
    p_similarity_threshold REAL DEFAULT 0.7
)
RETURNS TABLE(
    supplier_id INTEGER,
    company_name VARCHAR(255),
    similarity_score REAL,
    matched_text TEXT,
    country VARCHAR(100),
    rating INTEGER
) AS $$
DECLARE
    v_query_embedding VECTOR(1536);
BEGIN
    -- Generate embedding for search query
    SELECT ai.openai_embed('text-embedding-3-small', p_query, _dimensions => 1536)
    INTO v_query_embedding;

    -- Search using vectorscale for high performance
    RETURN QUERY
    SELECT 
        s.supplier_id,
        s.company_name,
        (se.embedding <=> v_query_embedding)::REAL as similarity_score,
        se.source_text as matched_text,
        s.country,
        s.rating
    FROM supplier_embeddings se
    JOIN suppliers s ON se.supplier_id = s.supplier_id
    WHERE 
        s.is_active = true
        AND se.embedding_type = 'profile'
        AND (se.embedding <=> v_query_embedding) < (1 - p_similarity_threshold)
    ORDER BY se.embedding <=> v_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;
```

---

## 📊 **Phase 3: Application Code Enhancement**

### **3.1 Enhanced Supplier Service**

**Update SupplierDataService.cs:**
```csharp
// Add new method for semantic search
public async Task<List<SupplierSemanticMatch>> SearchSuppliersSemanticAsync(
    string query, 
    int maxResults = 10, 
    float similarityThreshold = 0.7f)
{
    var sql = @"
        SELECT * FROM search_suppliers_semantic(@query, @limit, @threshold)";

    var results = await _context.Database
        .SqlQueryRaw<SupplierSemanticResult>(sql, 
            new NpgsqlParameter("@query", query),
            new NpgsqlParameter("@limit", maxResults),
            new NpgsqlParameter("@threshold", similarityThreshold))
        .ToListAsync();

    return results.Select(r => new SupplierSemanticMatch
    {
        SupplierId = r.SupplierId,
        CompanyName = r.CompanyName,
        SimilarityScore = r.SimilarityScore,
        MatchedText = r.MatchedText,
        Country = r.Country,
        Rating = r.Rating
    }).ToList();
}
```

### **3.2 New AI Chat Tools**

**Add to ChatController.cs:**
```csharp
[Description("Find suppliers that match specific project requirements using AI-powered semantic search")]
private async Task<string> FindSuppliersForRequirementsAsync(
    [Description("Describe the project requirements, needed capabilities, industry, or type of supplier")] 
    string requirements)
{
    try
    {
        var matches = await _supplierDataService
            .SearchSuppliersSemanticAsync(requirements, maxResults: 8);

        if (!matches.Any())
        {
            return $"No suppliers found matching requirements: '{requirements}'";
        }

        var response = new StringBuilder();
        response.AppendLine($"**Found {matches.Count} suppliers matching your requirements:**\n");

        foreach (var match in matches.Take(5))
        {
            var detailInfo = await _supplierDataService.GetSupplierByIdAsync(match.SupplierId);
            
            response.AppendLine($"**{detailInfo.CompanyName}** (Match: {match.SimilarityScore:P0})");
            response.AppendLine($"- Location: {detailInfo.Country}");
            
            if (detailInfo.Rating.HasValue)
                response.AppendLine($"- Rating: {detailInfo.Rating}/5 ⭐");
                
            if (detailInfo.Capabilities?.Any() == true)
            {
                var topCapabilities = detailInfo.Capabilities.Take(3)
                    .Select(c => $"{c.CapabilityType}: {c.CapabilityValue}")
                    .ToList();
                response.AppendLine($"- Key Capabilities: {string.Join(", ", topCapabilities)}");
            }
            
            response.AppendLine($"- Why matched: {TruncateText(match.MatchedText, 100)}");
            response.AppendLine();
        }

        return response.ToString();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in semantic supplier search for requirements: {Requirements}", requirements);
        return $"Error searching for suppliers: {ex.Message}";
    }
}

[Description("Find suppliers similar to a known supplier for alternative sourcing")]
private async Task<string> FindSimilarSuppliersAsync(
    [Description("Name or code of the reference supplier")] string referenceSupplier,
    [Description("Optional location preference like 'Europe', 'Asia', 'North America'")] string? locationPreference = null)
{
    try
    {
        // First get the reference supplier
        var referenceResults = await _supplierService.GetSuppliersAsync(1, 5, referenceSupplier, null, null, true);
        
        if (!referenceResults.Data.Any())
        {
            return $"Reference supplier '{referenceSupplier}' not found.";
        }

        var refSupplier = referenceResults.Data.First();
        var refDetail = await _supplierDataService.GetSupplierByIdAsync(refSupplier.SupplierId);
        
        // Build search query from reference supplier profile
        var searchQuery = $"Similar to {refDetail.CompanyName}";
        if (refDetail.Capabilities?.Any() == true)
        {
            var capabilities = string.Join(" ", refDetail.Capabilities.Select(c => c.CapabilityValue));
            searchQuery += $" with capabilities: {capabilities}";
        }

        // Add location filter if specified
        if (!string.IsNullOrEmpty(locationPreference))
        {
            searchQuery += $" located in {locationPreference}";
        }

        var similarSuppliers = await _supplierDataService
            .SearchSuppliersSemanticAsync(searchQuery, maxResults: 10);

        // Remove the reference supplier from results
        var alternatives = similarSuppliers
            .Where(s => s.SupplierId != refSupplier.SupplierId)
            .Take(5)
            .ToList();

        if (!alternatives.Any())
        {
            return $"No similar suppliers found to {refDetail.CompanyName}.";
        }

        var response = new StringBuilder();
        response.AppendLine($"**Found {alternatives.Count} suppliers similar to {refDetail.CompanyName}:**\n");

        foreach (var alt in alternatives)
        {
            var altDetail = await _supplierDataService.GetSupplierByIdAsync(alt.SupplierId);
            
            response.AppendLine($"**{altDetail.CompanyName}** (Similarity: {alt.SimilarityScore:P0})");
            response.AppendLine($"- Location: {altDetail.Country}");
            
            if (altDetail.Rating.HasValue)
                response.AppendLine($"- Rating: {altDetail.Rating}/5 ⭐");
                
            response.AppendLine($"- Similar because: {TruncateText(alt.MatchedText, 120)}");
            response.AppendLine();
        }

        return response.ToString();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error finding similar suppliers to {Reference}", referenceSupplier);
        return $"Error finding similar suppliers: {ex.Message}";
    }
}
```

---

## 📊 **Phase 4: Background Processing & Automation**

### **4.1 Embedding Generation Service**

**Create SupplierVectorizationService.cs:**
```csharp
public class SupplierVectorizationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SupplierVectorizationService> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateMissingEmbeddingsAsync();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Run hourly
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in supplier vectorization service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }

    private async Task GenerateMissingEmbeddingsAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ProcurementDbContext>();

        var sql = @"
            SELECT s.supplier_id
            FROM suppliers s
            LEFT JOIN supplier_embeddings se ON s.supplier_id = se.supplier_id 
                AND se.embedding_type = 'profile'
            WHERE s.is_active = true 
                AND (se.supplier_id IS NULL OR se.updated_at < s.updated_at)
            LIMIT 10";

        var suppliersNeedingEmbeddings = await context.Database
            .SqlQueryRaw<int>(sql)
            .ToListAsync();

        foreach (var supplierId in suppliersNeedingEmbeddings)
        {
            await context.Database.ExecuteSqlRawAsync(
                "SELECT generate_supplier_embedding(@supplierId)",
                new NpgsqlParameter("@supplierId", supplierId));
        }

        if (suppliersNeedingEmbeddings.Any())
        {
            _logger.LogInformation("Generated embeddings for {Count} suppliers", 
                suppliersNeedingEmbeddings.Count);
        }
    }
}
```

---

## 📊 **Phase 5: Migration Strategy**

### **5.1 Pre-Migration Checklist**
- [ ] Backup existing PostgreSQL data
- [ ] Test TimescaleDB container in development
- [ ] Verify pgvectorscale and pgai extension compatibility
- [ ] Update connection strings in appsettings
- [ ] Test embedding generation with sample data

### **5.2 Migration Steps**
1. **Backup**: Create full backup of existing database
2. **Deploy**: Start new TimescaleDB container alongside existing
3. **Schema**: Run new schema migration scripts
4. **Data**: Migrate existing supplier data
5. **Embeddings**: Generate initial embeddings for all active suppliers
6. **Test**: Verify semantic search functionality
7. **Switch**: Update application to use new database
8. **Cleanup**: Remove old PostgreSQL container

### **5.3 Rollback Plan**
- Keep old container running during initial deployment
- Maintain data synchronization scripts if needed
- Document exact steps to revert to previous setup

---

## 🎯 **Expected Benefits**

### **Immediate Gains**
- ✅ **Advanced Vector Search**: 10x faster than basic pgvector
- ✅ **AI-Powered Matching**: Natural language supplier discovery
- ✅ **Time-Series Performance**: Optimized supplier metrics tracking
- ✅ **Scalability**: TimescaleDB handles larger datasets efficiently

### **Strategic Advantages**
- 🧠 **Intelligent Procurement**: "Find suppliers for renewable energy projects"
- 🔍 **Smart Alternatives**: "Find suppliers similar to Acme Corp but in Europe"
- 📊 **Performance Analytics**: Historical supplier performance trends
- 🚀 **Future-Proofed**: Ready for advanced AI/ML procurement features

---

## 📋 **Implementation Timeline**

| Phase | Duration | Key Deliverables |
|-------|----------|------------------|
| **Phase 1** | 1-2 days | TimescaleDB container, extensions setup |
| **Phase 2** | 2-3 days | Enhanced schema, migration functions |
| **Phase 3** | 3-4 days | New API endpoints, semantic search |
| **Phase 4** | 2-3 days | Background processing, automation |
| **Phase 5** | 1-2 days | Migration execution, testing |

**Total Estimated Time**: 9-14 days

---

## 🚨 **Risk Mitigation**

### **Technical Risks**
- **Extension Compatibility**: Test pgvectorscale + pgai thoroughly
- **Performance**: Monitor query performance with vector operations
- **Data Migration**: Ensure complete data integrity during migration

### **Operational Risks**
- **Downtime**: Plan migration during low-usage periods
- **Rollback**: Maintain parallel systems until confident
- **Training**: Document new semantic search capabilities

---

## 🎉 **Success Metrics**

- ✅ **Query Performance**: Vector searches under 100ms
- ✅ **Search Quality**: 90%+ relevant results for natural language queries
- ✅ **User Adoption**: Increased usage of AI-powered supplier discovery
- ✅ **System Reliability**: 99.9% uptime with new infrastructure

This migration will transform your procurement platform from a traditional database-driven system into an intelligent AI-powered supplier discovery and matching platform! 🚀
