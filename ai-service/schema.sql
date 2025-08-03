-- AI Service Schema Additions
-- This file contains schema modifications needed for AI recommendation service

-- Enable pgvector extension for vector operations
CREATE EXTENSION IF NOT EXISTS vector;

-- Add vector embeddings to suppliers and items
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS embedding vector(1536);
ALTER TABLE items ADD COLUMN IF NOT EXISTS embedding vector(1536);

-- Create vector indexes for efficient similarity search
CREATE INDEX IF NOT EXISTS idx_suppliers_embedding ON suppliers USING ivfflat (embedding vector_cosine_ops);
CREATE INDEX IF NOT EXISTS idx_items_embedding ON items USING ivfflat (embedding vector_cosine_ops);

-- Supplier capabilities table for detailed AI matching
CREATE TABLE IF NOT EXISTS supplier_capabilities (
    capability_id SERIAL PRIMARY KEY,
    supplier_id INTEGER NOT NULL REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    capability_type VARCHAR(100) NOT NULL,
    capability_value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(supplier_id, capability_type, capability_value)
);

-- Item specifications table for detailed item matching
CREATE TABLE IF NOT EXISTS item_specifications (
    spec_id SERIAL PRIMARY KEY,
    item_id INTEGER NOT NULL REFERENCES items(item_id) ON DELETE CASCADE,
    spec_name VARCHAR(100) NOT NULL,
    spec_value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(item_id, spec_name, spec_value)
);

-- Create indexes for the new tables
CREATE INDEX IF NOT EXISTS idx_supplier_capabilities_supplier ON supplier_capabilities(supplier_id);
CREATE INDEX IF NOT EXISTS idx_supplier_capabilities_type ON supplier_capabilities(capability_type);
CREATE INDEX IF NOT EXISTS idx_item_specifications_item ON item_specifications(item_id);
CREATE INDEX IF NOT EXISTS idx_item_specifications_name ON item_specifications(spec_name);

-- Function to update embeddings (to be called by the AI service)
CREATE OR REPLACE FUNCTION update_supplier_embedding(
    p_supplier_id INTEGER,
    p_embedding vector(1536)
) RETURNS void AS $$
BEGIN
    UPDATE suppliers 
    SET embedding = p_embedding, updated_at = CURRENT_TIMESTAMP
    WHERE supplier_id = p_supplier_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION update_item_embedding(
    p_item_id INTEGER,
    p_embedding vector(1536)
) RETURNS void AS $$
BEGIN
    UPDATE items 
    SET embedding = p_embedding, updated_at = CURRENT_TIMESTAMP
    WHERE item_id = p_item_id;
END;
$$ LANGUAGE plpgsql;

-- Function to find similar suppliers based on embedding
CREATE OR REPLACE FUNCTION find_similar_suppliers(
    p_query_embedding vector(1536),
    p_limit INTEGER DEFAULT 10,
    p_similarity_threshold REAL DEFAULT 0.7
) RETURNS TABLE(
    supplier_id INTEGER,
    company_name VARCHAR(255),
    similarity REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        s.supplier_id,
        s.company_name,
        1 - (s.embedding <=> p_query_embedding) as similarity
    FROM suppliers s
    WHERE s.embedding IS NOT NULL
    AND s.is_active = true
    AND 1 - (s.embedding <=> p_query_embedding) >= p_similarity_threshold
    ORDER BY s.embedding <=> p_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;

-- Function to find similar items based on embedding
CREATE OR REPLACE FUNCTION find_similar_items(
    p_query_embedding vector(1536),
    p_limit INTEGER DEFAULT 10,
    p_similarity_threshold REAL DEFAULT 0.7
) RETURNS TABLE(
    item_id INTEGER,
    item_code VARCHAR(50),
    description TEXT,
    similarity REAL
) AS $$
BEGIN
    RETURN QUERY
    SELECT 
        i.item_id,
        i.item_code,
        i.description,
        1 - (i.embedding <=> p_query_embedding) as similarity
    FROM items i
    WHERE i.embedding IS NOT NULL
    AND i.is_active = true
    AND 1 - (i.embedding <=> p_query_embedding) >= p_similarity_threshold
    ORDER BY i.embedding <=> p_query_embedding
    LIMIT p_limit;
END;
$$ LANGUAGE plpgsql;
