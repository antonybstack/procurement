-- Items Table and Related Schema
-- This file contains the items table definition and related structures

-- Main items table
CREATE TABLE items (
    item_id SERIAL PRIMARY KEY,
    item_code VARCHAR(50) UNIQUE NOT NULL,
    description TEXT NOT NULL,
    category item_category NOT NULL,
    unit_of_measure VARCHAR(20) NOT NULL,
    standard_cost DECIMAL(15,2),
    min_order_quantity INTEGER DEFAULT 1,
    lead_time_days INTEGER DEFAULT 30,
    is_active BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    -- Vector embedding for item search
    embedding vector(768)
);

-- Item specifications table for detailed item matching
CREATE TABLE item_specifications (
    spec_id SERIAL PRIMARY KEY,
    item_id INTEGER NOT NULL REFERENCES items(item_id) ON DELETE CASCADE,
    spec_name VARCHAR(100) NOT NULL,
    spec_value TEXT NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(item_id, spec_name, spec_value)
);