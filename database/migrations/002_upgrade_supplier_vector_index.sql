-- Migration: Upgrade Supplier Vector Index to diskann
-- This migration replaces the ivfflat index with pgvectorscale's diskann index for suppliers

-- Drop the existing ivfflat index for suppliers
DROP INDEX IF EXISTS idx_suppliers_embedding;

-- Create new diskann index for suppliers with label filtering support
-- Note: This uses pgvectorscale's diskann algorithm for better performance
CREATE INDEX idx_suppliers_embedding_diskann ON suppliers 
USING diskann (embedding vector_cosine_ops)
WITH (
    num_neighbors = 50,
    search_list_size = 100,
    max_alpha = 1.2
);

-- Create a composite diskann index that supports both vector similarity and label filtering
-- This allows for efficient combined queries
CREATE INDEX idx_suppliers_embedding_category_diskann ON suppliers 
USING diskann (embedding vector_cosine_ops, category_labels)
WITH (
    num_neighbors = 50,
    search_list_size = 100
);

-- Add comment for documentation
COMMENT ON INDEX idx_suppliers_embedding_diskann IS 'Primary diskann vector index for supplier embeddings using pgvectorscale';
COMMENT ON INDEX idx_suppliers_embedding_category_diskann IS 'Composite diskann index for vector similarity with category filtering';