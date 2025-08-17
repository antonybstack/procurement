# TigerData AI Stack Migration Plan - Supplier Vectorization

## Overview

Migration from standard PostgreSQL 15 with pgvector to TigerData's AI stack featuring TimescaleDB, pgvectorscale, and pgai for enhanced AI application performance. This migration focuses specifically on the **suppliers table vectorization** to minimize scope and risk.

## Current System Analysis

### Database Setup

- **Database**: PostgreSQL 15-alpine with pgvector extension
- **Vector Storage**: Using vector(768) columns with ivfflat indexes
- **Vector Operations**: Manual embedding generation via OpenAI API
- **Search**: Raw SQL queries with cosine similarity
- **Migration Scope**: suppliers table only (other tables remain unchanged)

### Application Layer
- **.NET/C# API**: Using Semantic Kernel PgVector connector
- **Vector Service**: Manual embedding generation and storage
- **Search**: Basic vector similarity without advanced filtering

## Target TigerData AI Stack

### Core Components
1. **TimescaleDB**: Time-series optimized PostgreSQL with hypertables
2. **pgvectorscale**: High-performance vector extension with StreamingDiskANN indexing
3. **pgai**: Automated embedding pipeline with built-in vectorizers

### Key Benefits
- **Performance**: 28x lower p95 latency, 16x higher throughput vs traditional solutions
- **Efficiency**: 75% cost reduction with Statistical Binary Quantization (SBQ)
- **Automation**: Automatic embedding generation and synchronization
- **Advanced Search**: Label-based filtering and query-time parameter tuning

## Migration Requirements & Compatibility

### Database Extensions
```sql
-- Current extensions
CREATE EXTENSION IF NOT EXISTS vector;
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Additional required extensions for TigerData stack
CREATE EXTENSION IF NOT EXISTS timescaledb;
CREATE EXTENSION IF NOT EXISTS vectorscale;
-- pgai is installed via pip and creates 'ai' schema
```

### Index Migration
```sql
-- Current indexes (to be replaced)
CREATE INDEX idx_suppliers_embedding ON suppliers USING ivfflat (embedding vector_cosine_ops);

-- New pgvectorscale indexes
CREATE INDEX ON suppliers USING diskann (embedding vector_cosine_ops);
-- With label filtering support
CREATE INDEX ON suppliers USING diskann (embedding vector_cosine_ops, category_labels);
```

### Schema Changes Required

1. **Suppliers Table Enhancement**: Add label fields for efficient filtering
2. **pgai Schema**: Add ai schema for vectorizer configurations
3. **Index Optimization**: Replace ivfflat with diskann indexes for suppliers only

## Step-by-Step Migration Plan

### Phase 1: Environment Preparation (1-2 days)
1. **Docker Image Update**
   ```dockerfile
   # Replace postgres:15-alpine with TimescaleDB image
   FROM timescale/timescaledb:latest-pg15
   ```

2. **Extension Installation**
   ```bash
   # Install pgai in application environment
   pip install pgai
   
   # Install pgvectorscale (included in TimescaleDB image)
   ```

3. **Configuration Updates**
   ```yaml
   # docker-compose.yml
   services:
     postgres:
       image: timescale/timescaledb:latest-pg17
       environment:
         - POSTGRES_EXTENSION_LIST=timescaledb,vector,vectorscale
   ```

### Phase 2: Schema Migration (1-2 days)

1. **Create Migration Scripts**
   ```sql
   -- 001_add_timescale_extensions.sql
   CREATE EXTENSION IF NOT EXISTS timescaledb;
   CREATE EXTENSION IF NOT EXISTS vectorscale;
   
   -- 002_add_supplier_labels.sql
   ALTER TABLE suppliers ADD COLUMN category_labels TEXT[];
   ALTER TABLE suppliers ADD COLUMN certification_labels TEXT[];
   ALTER TABLE suppliers ADD COLUMN process_labels TEXT[];
   ```

2. **Index Recreation for Suppliers Only**
   ```sql
   -- Drop existing suppliers ivfflat index
   DROP INDEX idx_suppliers_embedding;
   
   -- Create diskann index for suppliers with label filtering
   CREATE INDEX ON suppliers USING diskann (embedding vector_cosine_ops, category_labels);
   ```

### Phase 3: pgai Integration (3-4 days)
1. **Install and Configure pgai**
   ```bash
   # Install pgai in application container
   pgai install -d "postgresql://postgres:password@localhost:5432/myapp"
   ```

2. **Create Supplier Vectorizer Only**
   ```sql
   -- Supplier vectorizer with enhanced context
   SELECT ai.create_vectorizer(
       'public.suppliers',
       embedding => ai.embedding_openai('text-embedding-3-small', 768),
       chunking => ai.chunking_character_text_splitter(1000),
       formatting => ai.formatting_python_template(
         'Company: $company_name, Contact: $contact_name, Country: $country, Rating: $rating, Capabilities: $capabilities'
       )
   );
   ```

### Phase 4: Application Layer Updates (2-3 days)
1. **Update .NET Dependencies**
   ```xml
   <!-- Remove Microsoft.SemanticKernel.Connectors.PgVector -->
   <!-- Add TimescaleDB/pgvectorscale .NET connector when available -->
   ```

2. **Refactor Supplier Vector Services Only**
   ```csharp
   // Update VectorStoreService to use pgai for suppliers
   // Keep manual embedding for other tables (out of scope)
   // Implement label-based filtering for suppliers
   ```

3. **Update Supplier Search Queries**
   ```sql
   -- Enhanced supplier search with label filtering
   SELECT * FROM suppliers
   WHERE category_labels && ARRAY['electronics', 'manufacturing']
     AND certification_labels && ARRAY['ISO 9001', 'AS9100']
   ORDER BY embedding <=> $1
   LIMIT 10;
   ```

### Phase 5: Data Migration & Testing (3-4 days)
1. **Backup Current Data**
   ```bash
   pg_dump myapp > backup_pre_migration.sql
   ```

2. **Migrate Supplier Data Only**
   ```sql
   -- Populate supplier label fields from existing capabilities
   UPDATE suppliers SET 
     category_labels = ARRAY(
       SELECT capability_value FROM supplier_capabilities 
       WHERE supplier_id = suppliers.supplier_id 
         AND capability_type = 'Material'
     ),
     certification_labels = ARRAY(
       SELECT capability_value FROM supplier_capabilities 
       WHERE supplier_id = suppliers.supplier_id 
         AND capability_type = 'Certification'
     ),
     process_labels = ARRAY(
       SELECT capability_value FROM supplier_capabilities 
       WHERE supplier_id = suppliers.supplier_id 
         AND capability_type = 'Process'
     );
   ```

3. **Performance Testing**
   - Supplier vector search performance comparison
   - Label filtering efficiency tests for suppliers
   - Supplier embedding generation automation verification

### Phase 6: Production Deployment (1-2 days)
1. **Blue-Green Deployment**
   - Deploy new stack in parallel environment
   - Switch traffic after verification
   - Monitor performance metrics

2. **Monitoring Setup**
   ```sql
   -- TimescaleDB monitoring views
   SELECT * FROM timescaledb_information.hypertables;
   SELECT * FROM timescaledb_information.chunks;
   ```

## Rollback Strategy

### Immediate Rollback (< 1 hour)
1. **Docker Compose Rollback**
   ```bash
   # Switch back to original docker-compose.yml
   docker-compose down
   git checkout HEAD~1 docker-compose.yml
   docker-compose up -d
   ```

2. **Database Restore**
   ```bash
   # Restore from backup
   dropdb myapp
   createdb myapp
   psql myapp < backup_pre_migration.sql
   ```

### Partial Rollback Options
1. **Disable pgai vectorizers** while keeping TimescaleDB
2. **Revert to ivfflat indexes** while keeping hypertables
3. **Gradual rollback** of individual components

## Testing Approach

### Pre-Migration Testing
1. **Performance Baseline**
   - Vector search response times
   - Embedding generation throughput
   - Query performance metrics

2. **Functional Testing**
   - All existing API endpoints
   - Search accuracy validation
   - Data integrity checks

### Post-Migration Validation
1. **Performance Verification**
   - Compare search performance (target: 28x improvement)
   - Verify automatic embedding generation
   - Test label-based filtering

2. **Data Integrity**
   - Vector similarity preservation
   - Record count validation
   - Business logic verification

### Load Testing
1. **Concurrent Search Queries**
2. **Embedding Generation Under Load**
3. **Mixed Workload Performance**

## Risk Mitigation

### High-Risk Items
1. **Docker Image Changes**: Test thoroughly in staging
2. **Index Recreation**: Plan for downtime during index rebuilding
3. **Application Code Changes**: Implement feature flags for gradual rollout

### Monitoring & Alerts
1. **Performance Degradation**: Set alerts for query response times
2. **Embedding Generation Failures**: Monitor pgai vectorizer health
3. **Data Consistency**: Implement integrity checks

## Timeline Summary
- **Total Duration**: 8-12 days (reduced scope)
- **Critical Path**: Schema migration → pgai integration → supplier service updates  
- **Parallel Workstreams**: Testing, monitoring setup, documentation

## Success Criteria
1. **Performance**: 10x+ improvement in supplier search response times
2. **Automation**: Automatic embedding generation for suppliers working reliably
3. **Functionality**: All existing supplier search features working without degradation
4. **Reliability**: < 0.1% error rate in supplier vectorization
5. **Scope**: Only suppliers table migrated, other tables unchanged

## Next Steps
1. **Approval**: Get stakeholder approval for migration plan
2. **Environment Setup**: Prepare staging environment
3. **Team Training**: Train development team on new technologies
4. **Migration Schedule**: Schedule migration window with stakeholders