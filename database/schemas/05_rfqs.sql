-- Request for Quotes (RFQ) Tables and Related Schema
-- This file contains all RFQ-related table definitions

-- Main request for quotes table
CREATE TABLE request_for_quotes (
    rfq_id SERIAL PRIMARY KEY,
    rfq_number VARCHAR(50) UNIQUE NOT NULL,
    title VARCHAR(255) NOT NULL,
    description TEXT,
    status rfq_status DEFAULT 'draft',
    issue_date DATE NOT NULL,
    due_date DATE NOT NULL,
    award_date DATE,
    total_estimated_value DECIMAL(15,2),
    currency VARCHAR(3) DEFAULT 'USD',
    terms_and_conditions TEXT,
    created_by VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    -- Vector embedding for RFQ search
    embedding vector(768)
);

-- RFQ line items table
CREATE TABLE rfq_line_items (
    line_item_id SERIAL PRIMARY KEY,
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id) ON DELETE CASCADE,
    item_id INTEGER REFERENCES items(item_id),
    line_number INTEGER NOT NULL,
    quantity_required INTEGER NOT NULL,
    unit_of_measure VARCHAR(20) NOT NULL,
    description TEXT,
    technical_specifications TEXT,
    delivery_date DATE,
    estimated_unit_cost DECIMAL(15,2),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(rfq_id, line_number),
    -- Vector embedding for line item search
    embedding vector(768)
);

-- RFQ suppliers junction table
CREATE TABLE rfq_suppliers (
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id) ON DELETE CASCADE,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    invited_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    response_date TIMESTAMP WITH TIME ZONE,
    is_responded BOOLEAN DEFAULT false,
    PRIMARY KEY (rfq_id, supplier_id)
);