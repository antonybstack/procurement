-- Quotes Table and Related Schema
-- This file contains the quotes table definition

-- Main quotes table
CREATE TABLE quotes (
    quote_id SERIAL PRIMARY KEY,
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id) ON DELETE CASCADE,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    line_item_id INTEGER REFERENCES rfq_line_items(line_item_id) ON DELETE CASCADE,
    quote_number VARCHAR(50) NOT NULL,
    status quote_status DEFAULT 'pending',
    unit_price DECIMAL(15,2) NOT NULL,
    total_price DECIMAL(15,2) NOT NULL,
    quantity_offered INTEGER NOT NULL,
    delivery_date DATE,
    payment_terms VARCHAR(100),
    warranty_period_months INTEGER,
    technical_compliance_notes TEXT,
    submitted_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    valid_until_date DATE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    -- Vector embedding for quote search
    embedding vector(768)
);