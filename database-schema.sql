-- Database Schema for AI Recommendation Service
-- This file contains the schema information that the AI service uses to understand the database structure

-- Tables and their relationships for supplier recommendation

-- Suppliers table
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
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Items table (parts/products)
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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Request for Quotes table
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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- RFQ Line Items table
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
    UNIQUE(rfq_id, line_number)
);

-- RFQ Suppliers table (which suppliers are invited to RFQs)
CREATE TABLE rfq_suppliers (
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id) ON DELETE CASCADE,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    invited_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    response_date TIMESTAMP WITH TIME ZONE,
    is_responded BOOLEAN DEFAULT false,
    PRIMARY KEY (rfq_id, supplier_id)
);

-- Quotes table (supplier responses to RFQ line items)
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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Purchase Orders table
CREATE TABLE purchase_orders (
    po_id SERIAL PRIMARY KEY,
    po_number VARCHAR(50) UNIQUE NOT NULL,
    supplier_id INTEGER REFERENCES suppliers(supplier_id),
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id),
    status po_status DEFAULT 'draft',
    order_date DATE NOT NULL,
    expected_delivery_date DATE,
    total_amount DECIMAL(15,2) NOT NULL,
    currency VARCHAR(3) DEFAULT 'USD',
    payment_terms VARCHAR(100),
    shipping_address TEXT,
    billing_address TEXT,
    notes TEXT,
    created_by VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Purchase Order Lines table
CREATE TABLE purchase_order_lines (
    po_line_id SERIAL PRIMARY KEY,
    po_id INTEGER REFERENCES purchase_orders(po_id) ON DELETE CASCADE,
    quote_id INTEGER REFERENCES quotes(quote_id),
    line_number INTEGER NOT NULL,
    item_id INTEGER NOT NULL REFERENCES items(item_id),
    quantity_ordered INTEGER NOT NULL,
    unit_price DECIMAL(15,2) NOT NULL,
    total_price DECIMAL(15,2) NOT NULL,
    delivery_date DATE,
    description TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(po_id, line_number)
);

-- Enum types
CREATE TYPE quote_status AS ENUM ('pending', 'submitted', 'awarded', 'rejected', 'expired');
CREATE TYPE rfq_status AS ENUM ('draft', 'published', 'closed', 'awarded', 'cancelled');
CREATE TYPE po_status AS ENUM ('draft', 'sent', 'confirmed', 'received', 'cancelled');
CREATE TYPE item_category AS ENUM ('electronics', 'machinery', 'raw_materials', 'packaging', 'services', 'components');

-- Key indexes for performance
CREATE INDEX idx_suppliers_company_name ON suppliers(company_name);
CREATE INDEX idx_suppliers_country ON suppliers(country);
CREATE INDEX idx_suppliers_rating ON suppliers(rating);
CREATE INDEX idx_items_category ON items(category);
CREATE INDEX idx_items_item_code ON items(item_code);
CREATE INDEX idx_quotes_supplier ON quotes(supplier_id);
CREATE INDEX idx_quotes_rfq ON quotes(rfq_id);
CREATE INDEX idx_quotes_status ON quotes(status);
CREATE INDEX idx_rfq_line_items_item ON rfq_line_items(item_id);

-- Views for common queries
CREATE OR REPLACE VIEW supplier_performance AS
SELECT 
    s.supplier_id,
    s.company_name,
    s.rating,
    s.country,
    COUNT(DISTINCT q.quote_id) as total_quotes,
    COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) as awarded_quotes,
    AVG(q.unit_price) as avg_quote_price,
    COUNT(DISTINCT po.po_id) as total_purchase_orders,
    SUM(po.total_amount) as total_po_value
FROM suppliers s
LEFT JOIN quotes q ON s.supplier_id = q.supplier_id
LEFT JOIN purchase_orders po ON s.supplier_id = po.supplier_id
WHERE s.is_active = true
GROUP BY s.supplier_id, s.company_name, s.rating, s.country;

CREATE OR REPLACE VIEW item_supplier_history AS
SELECT 
    i.item_id,
    i.item_code,
    i.description,
    i.category,
    s.supplier_id,
    s.company_name,
    COUNT(q.quote_id) as quote_count,
    AVG(q.unit_price) as avg_price,
    MIN(q.unit_price) as min_price,
    MAX(q.unit_price) as max_price,
    COUNT(CASE WHEN q.status = 'awarded' THEN 1 END) as awarded_count
FROM items i
LEFT JOIN rfq_line_items rli ON i.item_id = rli.item_id
LEFT JOIN quotes q ON rli.line_item_id = q.line_item_id
LEFT JOIN suppliers s ON q.supplier_id = s.supplier_id
WHERE i.is_active = true AND s.is_active = true
GROUP BY i.item_id, i.item_code, i.description, i.category, s.supplier_id, s.company_name; 