-- Migration: Add Label Fields to Suppliers Table
-- This migration adds categorical label fields to support efficient filtering in pgvectorscale

-- Add label columns to suppliers table for enhanced filtering
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS category_labels TEXT[];
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS certification_labels TEXT[];
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS process_labels TEXT[];
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS material_labels TEXT[];
ALTER TABLE suppliers ADD COLUMN IF NOT EXISTS service_labels TEXT[];

-- Create indexes for label-based filtering
CREATE INDEX IF NOT EXISTS idx_suppliers_category_labels ON suppliers USING GIN (category_labels);
CREATE INDEX IF NOT EXISTS idx_suppliers_certification_labels ON suppliers USING GIN (certification_labels);
CREATE INDEX IF NOT EXISTS idx_suppliers_process_labels ON suppliers USING GIN (process_labels);
CREATE INDEX IF NOT EXISTS idx_suppliers_material_labels ON suppliers USING GIN (material_labels);
CREATE INDEX IF NOT EXISTS idx_suppliers_service_labels ON suppliers USING GIN (service_labels);

-- Populate label fields from existing supplier_capabilities data
UPDATE suppliers SET 
    certification_labels = (
        SELECT ARRAY_AGG(capability_value) 
        FROM supplier_capabilities 
        WHERE supplier_id = suppliers.supplier_id 
          AND capability_type = 'Certification'
    ),
    process_labels = (
        SELECT ARRAY_AGG(capability_value) 
        FROM supplier_capabilities 
        WHERE supplier_id = suppliers.supplier_id 
          AND capability_type = 'Process'
    ),
    material_labels = (
        SELECT ARRAY_AGG(capability_value) 
        FROM supplier_capabilities 
        WHERE supplier_id = suppliers.supplier_id 
          AND capability_type = 'Material'
    ),
    service_labels = (
        SELECT ARRAY_AGG(capability_value) 
        FROM supplier_capabilities 
        WHERE supplier_id = suppliers.supplier_id 
          AND capability_type = 'Service'
    ),
    category_labels = (
        -- Create category labels based on country and rating for testing
        CASE 
            WHEN country = 'USA' AND rating >= 4 THEN ARRAY['domestic', 'high-quality']
            WHEN country = 'USA' THEN ARRAY['domestic']
            WHEN rating >= 4 THEN ARRAY['international', 'high-quality']
            ELSE ARRAY['international']
        END
    );