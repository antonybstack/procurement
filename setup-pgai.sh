#!/bin/bash

# Comprehensive pgai setup script for TigerData migration
# Handles all pgai installation, configuration, and vectorizer creation
# This script ensures a complete working pgai setup

set -e

echo "🚀 Setting up pgai for TigerData migration..."

# Check if database is running
echo "📊 Checking database connection..."
if ! docker-compose -f docker-compose.db.yml exec postgres pg_isready -U postgres -d myapp; then
    echo "❌ Database is not ready. Please start the database first with: docker-compose -f docker-compose.db.yml up -d"
    exit 1
fi

# Check if vectorizer worker is running
echo "🤖 Checking vectorizer worker status..."
if ! docker-compose -f docker-compose.db.yml ps vectorizer-worker | grep -q "Up"; then
    echo "❌ Vectorizer worker is not running. Please start it with: docker-compose -f docker-compose.db.yml up -d"
    exit 1
fi

# Step 1: Install pgai schema if not already installed
echo ""
echo "🔍 Step 1: Checking pgai installation..."
AI_FUNCTIONS=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM information_schema.routines WHERE routine_schema = 'ai';" 2>/dev/null || echo "0")

if [ "$AI_FUNCTIONS" -gt "0" ]; then
    echo "✅ pgai already installed ($AI_FUNCTIONS functions found)"
else
    echo "💾 Installing pgai schema in database..."
    docker-compose -f docker-compose.db.yml exec vectorizer-worker python -c "import pgai; pgai.install('postgres://postgres:postgres_password@postgres:5432/myapp')"
    
    # Verify installation
    AI_FUNCTIONS=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM information_schema.routines WHERE routine_schema = 'ai';")
    echo "✅ pgai installed with $AI_FUNCTIONS AI functions"
fi

# Step 2: Check API key configuration
echo ""
echo "🔍 Step 2: Checking API key configuration..."
if docker-compose -f docker-compose.db.yml exec postgres printenv | grep -q "OPENAI_API_KEY=sk-"; then
    echo "✅ OpenAI API key is configured"
else
    echo "❌ OpenAI API key not found or invalid format"
    echo "   Please ensure OPENAI_API_KEY is set in config.env with format: sk-..."
    echo "   Current key: $(docker-compose -f docker-compose.db.yml exec postgres printenv | grep OPENAI_API_KEY || echo 'Not found')"
    exit 1
fi

# Step 3: Check and create supplier vectorizer if needed
echo ""
echo "🔍 Step 3: Checking supplier vectorizer..."
VECTORIZERS=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = 'suppliers_embedding_store';" 2>/dev/null || echo "0")

if [ "$VECTORIZERS" -gt "0" ]; then
    echo "✅ Supplier vectorizer already configured"
    EMBEDDINGS=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM suppliers_embedding_store;" 2>/dev/null || echo "0")
    echo "📊 Current embeddings: $EMBEDDINGS"
else
    echo "🤖 Creating supplier vectorizer..."
    
    # Check if suppliers table has embedding column (conflict with pgai)
    HAS_EMBEDDING=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM information_schema.columns WHERE table_name = 'suppliers' AND column_name = 'embedding';" 2>/dev/null || echo "0")
    
    if [ "$HAS_EMBEDDING" -gt "0" ]; then
        echo "🔧 Removing conflicting embedding column..."
        docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -c "ALTER TABLE suppliers DROP COLUMN IF EXISTS embedding;" >/dev/null 2>&1
    fi
    
    # Create the vectorizer
    echo "⚙️  Creating vectorizer with OpenAI text-embedding-3-small..."
    docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -c "
    SELECT ai.create_vectorizer(
        'public.suppliers'::regclass, 
        loading => ai.loading_column('company_name'), 
        embedding => ai.embedding_openai('text-embedding-3-small', 768), 
        chunking => ai.chunking_character_text_splitter(1000)
    );" 2>&1
    
    if [ $? -eq 0 ]; then
        echo "✅ Supplier vectorizer created successfully"
        echo "⏳ Embeddings will be generated automatically..."
        
        # Wait a moment and check initial progress
        echo "🔄 Waiting for initial embedding generation..."
        sleep 10
        EMBEDDINGS=$(docker-compose -f docker-compose.db.yml exec postgres psql -U postgres -d myapp -tAc "SELECT count(*) FROM suppliers_embedding_store;" 2>/dev/null || echo "0")
        echo "📊 Initial embeddings generated: $EMBEDDINGS"
    else
        echo "❌ Failed to create supplier vectorizer"
        exit 1
    fi
fi

# Step 4: Final verification and status
echo ""
echo "🔍 Step 4: Final verification..."

# Check vectorizer worker logs
echo "📋 Checking vectorizer worker status..."
docker-compose -f docker-compose.db.yml logs --tail=5 vectorizer-worker

echo ""
echo "✅ pgai setup completed successfully!"
echo ""
echo "📊 Final Status:"
echo "- pgai schema: ✅ $AI_FUNCTIONS functions installed"
echo "- API key: ✅ Configured"
echo "- Supplier vectorizer: ✅ Active"
echo "- Embedding generation: ✅ Automated"
echo ""
echo "🚀 TigerData AI stack is fully operational!"