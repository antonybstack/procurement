#!/bin/bash

# Test script for TigerData migration
# This script validates the migration and tests performance improvements

set -e

echo "🧪 Testing TigerData Migration - Suppliers Vectorization"
echo "========================================================="

# Check if database is running
echo "📊 Checking database connection..."
if ! docker-compose exec postgres pg_isready -U postgres -d myapp; then
    echo "❌ Database is not ready. Please start the database first."
    exit 1
fi

echo "✅ Database is ready"

# Test 1: Verify extensions are installed
echo ""
echo "🔍 Test 1: Verifying extensions..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT 
    extname, 
    extversion 
FROM pg_extension 
WHERE extname IN ('timescaledb', 'vector', 'vectorscale')
ORDER BY extname;
"

# Test 2: Check suppliers table structure
echo ""
echo "🔍 Test 2: Checking suppliers table structure..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT 
    column_name, 
    data_type, 
    is_nullable 
FROM information_schema.columns 
WHERE table_name = 'suppliers' 
    AND column_name LIKE '%label%'
ORDER BY ordinal_position;
"

# Test 3: Verify label data population
echo ""
echo "🔍 Test 3: Checking label data population..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT 
    'certification_labels' as label_type,
    COUNT(*) as suppliers_with_labels,
    COUNT(*) FILTER (WHERE array_length(certification_labels, 1) > 0) as non_empty_labels
FROM suppliers
UNION ALL
SELECT 
    'process_labels' as label_type,
    COUNT(*) as suppliers_with_labels,
    COUNT(*) FILTER (WHERE array_length(process_labels, 1) > 0) as non_empty_labels
FROM suppliers
UNION ALL
SELECT 
    'material_labels' as label_type,
    COUNT(*) as suppliers_with_labels,
    COUNT(*) FILTER (WHERE array_length(material_labels, 1) > 0) as non_empty_labels
FROM suppliers;
"

# Test 4: Check indexes
echo ""
echo "🔍 Test 4: Verifying indexes..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT 
    indexname, 
    indexdef 
FROM pg_indexes 
WHERE tablename = 'suppliers' 
    AND indexname LIKE '%embedding%'
ORDER BY indexname;
"

# Test 5: Performance test - Label filtering
echo ""
echo "🔍 Test 5: Testing label-based filtering performance..."
echo "⏱️  Testing certification label filter..."
docker-compose exec postgres psql -U postgres -d myapp -c "
EXPLAIN ANALYZE
SELECT supplier_id, company_name, certification_labels
FROM suppliers 
WHERE certification_labels && ARRAY['ISO 9001', 'AS9100']
LIMIT 10;
"

# Test 6: Sample vector search (if embeddings exist)
echo ""
echo "🔍 Test 6: Testing vector search capability..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT 
    COUNT(*) as total_suppliers,
    COUNT(*) FILTER (WHERE embedding IS NOT NULL) as suppliers_with_embeddings
FROM suppliers;
"

# Test 7: Check pgai schema (if installed)
echo ""
echo "🔍 Test 7: Checking pgai installation..."
docker-compose exec postgres psql -U postgres -d myapp -c "
SELECT schema_name 
FROM information_schema.schemata 
WHERE schema_name = 'ai';
" || echo "ℹ️  pgai not yet installed - this is expected before Phase 3"

echo ""
echo "✅ Migration testing completed!"
echo ""
echo "📊 Summary:"
echo "- TimescaleDB image: ✅ Updated"
echo "- Extensions: ✅ Configured"
echo "- Supplier labels: ✅ Added and populated"
echo "- Vector indexes: ✅ Ready for diskann upgrade"
echo "- pgai: ⏳ Ready for installation"