-- Procurement RFQ Database Schema
-- This comprehensive script creates a complete procurement database with RFQ functionality and AI vectorization support

-- Create extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";
CREATE EXTENSION IF NOT EXISTS vector;

-- Create enum types
CREATE TYPE quote_status AS ENUM ('pending', 'submitted', 'awarded', 'rejected', 'expired');
CREATE TYPE rfq_status AS ENUM ('draft', 'published', 'closed', 'awarded', 'cancelled');
CREATE TYPE po_status AS ENUM ('draft', 'sent', 'confirmed', 'received', 'cancelled');
CREATE TYPE item_category AS ENUM ('electronics', 'machinery', 'raw_materials', 'packaging', 'services', 'components');

-- Create tables
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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    -- Vector embedding for supplier search
    embedding vector(768)
);

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

CREATE TABLE rfq_suppliers (
    rfq_id INTEGER REFERENCES request_for_quotes(rfq_id) ON DELETE CASCADE,
    supplier_id INTEGER REFERENCES suppliers(supplier_id) ON DELETE CASCADE,
    invited_date TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    response_date TIMESTAMP WITH TIME ZONE,
    is_responded BOOLEAN DEFAULT false,
    PRIMARY KEY (rfq_id, supplier_id)
);

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
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    -- Vector embedding for PO search
    embedding vector(768)
);

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
    UNIQUE(po_id, line_number),
    -- Vector embedding for PO line search
    embedding vector(768)
);

-- AI-related table creation
-- These tables support the AI recommendation features by storing vector embeddings and detailed attributes.

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

-- Create indexes for performance
CREATE INDEX idx_suppliers_company_name ON suppliers(company_name);
CREATE INDEX idx_suppliers_country ON suppliers(country);
CREATE INDEX idx_suppliers_rating ON suppliers(rating);
CREATE INDEX idx_items_category ON items(category);
CREATE INDEX idx_items_item_code ON items(item_code);
CREATE INDEX idx_rfq_status ON request_for_quotes(status);
CREATE INDEX idx_rfq_due_date ON request_for_quotes(due_date);
CREATE INDEX idx_quotes_status ON quotes(status);
CREATE INDEX idx_quotes_supplier ON quotes(supplier_id);
CREATE INDEX idx_quotes_rfq ON quotes(rfq_id);
CREATE INDEX idx_quotes_line_item ON quotes(line_item_id);
CREATE INDEX idx_rfq_line_items_rfq ON rfq_line_items(rfq_id);
CREATE INDEX idx_rfq_line_items_item ON rfq_line_items(item_id);
CREATE INDEX idx_po_supplier ON purchase_orders(supplier_id);
CREATE INDEX idx_po_status ON purchase_orders(status);

-- Vector indexes for semantic search
CREATE INDEX idx_suppliers_embedding ON suppliers USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_items_embedding ON items USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_rfqs_embedding ON request_for_quotes USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_rfq_line_items_embedding ON rfq_line_items USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_quotes_embedding ON quotes USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_purchase_orders_embedding ON purchase_orders USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_purchase_order_lines_embedding ON purchase_order_lines USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);

-- Create a function to update the updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers for updated_at columns
CREATE TRIGGER update_suppliers_updated_at BEFORE UPDATE ON suppliers FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_items_updated_at BEFORE UPDATE ON items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_rfq_updated_at BEFORE UPDATE ON request_for_quotes FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_rfq_line_items_updated_at BEFORE UPDATE ON rfq_line_items FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_quotes_updated_at BEFORE UPDATE ON quotes FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_po_updated_at BEFORE UPDATE ON purchase_orders FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
CREATE TRIGGER update_po_lines_updated_at BEFORE UPDATE ON purchase_order_lines FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Create views for monitoring and reporting
CREATE OR REPLACE VIEW active_connections AS
SELECT 
    datname,
    usename,
    application_name,
    client_addr,
    state,
    query_start,
    state_change
FROM pg_stat_activity 
WHERE state IS NOT NULL;

CREATE OR REPLACE VIEW rfq_summary AS
SELECT 
    rfq.rfq_id,
    rfq.rfq_number,
    rfq.title,
    rfq.status,
    rfq.issue_date,
    rfq.due_date,
    COUNT(DISTINCT rli.line_item_id) as line_items_count,
    COUNT(DISTINCT rs.supplier_id) as suppliers_invited,
    COUNT(DISTINCT q.quote_id) as quotes_received,
    rfq.total_estimated_value
FROM request_for_quotes rfq
LEFT JOIN rfq_line_items rli ON rfq.rfq_id = rli.rfq_id
LEFT JOIN rfq_suppliers rs ON rfq.rfq_id = rs.rfq_id
LEFT JOIN quotes q ON rfq.rfq_id = q.rfq_id
GROUP BY rfq.rfq_id, rfq.rfq_number, rfq.title, rfq.status, rfq.issue_date, rfq.due_date, rfq.total_estimated_value;

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

-- Generate sample data
-- Insert 1000 suppliers
INSERT INTO suppliers (supplier_code, company_name, contact_name, email, phone, address, city, state, country, postal_code, tax_id, payment_terms, credit_limit, rating)
SELECT 
    'SUP' || LPAD(s.id::text, 6, '0') as supplier_code,
    'Supplier ' || s.id || ' Corp.' as company_name,
    'Contact ' || s.id as contact_name,
    'contact' || s.id || '@supplier' || s.id || '.com' as email,
    '+1-555-' || LPAD((s.id % 999)::text, 3, '0') || '-' || LPAD((s.id % 9999)::text, 4, '0') as phone,
    s.id || ' Main Street' as address,
    (ARRAY['New York', 'Los Angeles', 'Chicago', 'Houston', 'Phoenix', 'Philadelphia', 'San Antonio', 'San Diego', 'Dallas', 'San Jose'])[(s.id % 10) + 1] as city,
    (ARRAY['NY', 'CA', 'IL', 'TX', 'AZ', 'PA', 'TX', 'CA', 'TX', 'CA'])[(s.id % 10) + 1] as state,
    'USA' as country,
    LPAD((s.id % 99999)::text, 5, '0') as postal_code,
    'TAX' || LPAD(s.id::text, 8, '0') as tax_id,
    (ARRAY['Net 30', 'Net 45', 'Net 60', '2/10 Net 30'])[(s.id % 4) + 1] as payment_terms,
    (s.id % 1000000 + 50000)::decimal(15,2) as credit_limit,
    (s.id % 5 + 1) as rating
FROM generate_series(1, 1000) s(id);

-- Generate meaningful supplier capabilities
-- This section populates the supplier_capabilities table with realistic data
-- to enable effective AI-driven supplier recommendations.

-- Clear existing capabilities to ensure a fresh start
DELETE FROM supplier_capabilities;

-- Define capability clusters for different supplier types
-- Each cluster includes a set of certifications, processes, and materials.

-- Cluster 1: Aerospace & Defense Specialists (Suppliers 1-200)
-- High-precision, certified for aerospace standards.
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Certification',
    (ARRAY['AS9100', 'ISO 9001', 'NADCAP'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(1, 200) s(id);

INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Process',
    (ARRAY['CNC Machining (5-axis)', 'Titanium Alloying', 'Composites Manufacturing'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(1, 200) s(id);

INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Material',
    (ARRAY['Aluminum 7075', 'Titanium Grade 5', 'Carbon Fiber Pre-preg'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(1, 200) s(id);

-- Cluster 2: Electronics & Components (Suppliers 201-400)
-- Specialized in electronics manufacturing and assembly.
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Certification',
    (ARRAY['ISO 13485 (Medical)', 'IPC-A-610', 'RoHS Compliant'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(201, 400) s(id);

INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Process',
    (ARRAY['PCB Assembly', 'Surface Mount Technology (SMT)', 'Automated Optical Inspection'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(201, 400) s(id);

-- Cluster 3: Raw Materials & Metals (Suppliers 401-600)
-- Bulk suppliers of raw materials and standardized metals.
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Certification',
    (ARRAY['ISO 14001', 'Conflict-Free Minerals'])[ ((s.id + FLOOR(random() * 2))::integer) % 2 + 1 ]
FROM generate_series(401, 600) s(id);

INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Material',
    (ARRAY['Stainless Steel 316', 'Aluminum 6061', 'Cold Rolled Steel'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(401, 600) s(id);

-- Cluster 4: General Purpose Machining & Fabrication (Suppliers 601-800)
-- Standard machining and fabrication services.
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Certification',
    'ISO 9001'
FROM generate_series(601, 800) s(id);

INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Process',
    (ARRAY['CNC Machining (3-axis)', 'Welding', 'Sheet Metal Fabrication'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(601, 800) s(id);

-- Cluster 5: Specialized Services (Suppliers 801-1000)
-- Suppliers offering specialized services like finishing, testing, etc.
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Service',
    (ARRAY['Anodizing', 'Heat Treatment', 'Non-Destructive Testing'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(801, 1000) s(id);

-- Insert 100 items
INSERT INTO items (item_code, description, category, unit_of_measure, standard_cost, min_order_quantity, lead_time_days)
SELECT 
    'ITEM' || LPAD(s.id::text, 6, '0') as item_code,
    (ARRAY['Microprocessor', 'Memory Module', 'Hard Drive', 'Power Supply', 'Motherboard', 'Graphics Card', 'Network Card', 'Sound Card', 'Cooling Fan', 'Cable Assembly'])[(s.id % 10) + 1] || ' - Model ' || s.id as description,
    (ARRAY['electronics', 'electronics', 'electronics', 'electronics', 'electronics', 'electronics', 'electronics', 'electronics', 'components', 'components'])[(s.id % 10) + 1]::item_category as category,
    (ARRAY['EA', 'EA', 'EA', 'EA', 'EA', 'EA', 'EA', 'EA', 'EA', 'FT'])[(s.id % 10) + 1] as unit_of_measure,
    (s.id % 1000 + 50)::decimal(15,2) as standard_cost,
    (s.id % 10 + 1) as min_order_quantity,
    (s.id % 30 + 15) as lead_time_days
FROM generate_series(1, 100) s(id);

-- Insert 1000 RFQs
INSERT INTO request_for_quotes (rfq_number, title, description, status, issue_date, due_date, total_estimated_value, created_by)
SELECT 
    'RFQ-' || LPAD(s.id::text, 6, '0') as rfq_number,
    'Procurement for ' || (ARRAY['Electronics Components', 'Machinery Parts', 'Raw Materials', 'Packaging Supplies', 'IT Services', 'Maintenance Services', 'Office Supplies', 'Safety Equipment', 'Tools and Equipment', 'Software Licenses'])[(s.id % 10) + 1] as title,
    'Request for quotation for ' || (ARRAY['Electronics Components', 'Machinery Parts', 'Raw Materials', 'Packaging Supplies', 'IT Services', 'Maintenance Services', 'Office Supplies', 'Safety Equipment', 'Tools and Equipment', 'Software Licenses'])[(s.id % 10) + 1] || ' - Project ' || s.id as description,
    (ARRAY['draft', 'published', 'closed', 'awarded'])[(s.id % 4) + 1]::rfq_status as status,
    CURRENT_DATE - (s.id % 365)::integer as issue_date,
    CURRENT_DATE + (s.id % 30 + 7)::integer as due_date,
    (s.id % 100000 + 10000)::decimal(15,2) as total_estimated_value,
    'User ' || (s.id % 10 + 1) as created_by
FROM generate_series(1, 1000) s(id);

-- Insert 10000 RFQ line items (10 per RFQ)
INSERT INTO rfq_line_items (rfq_id, item_id, line_number, quantity_required, unit_of_measure, description, delivery_date, estimated_unit_cost)
SELECT 
    ((s.id - 1) / 10) + 1 as rfq_id,
    (s.id % 100) + 1 as item_id,
    ((s.id - 1) % 10) + 1 as line_number,
    (s.id % 1000 + 10) as quantity_required,
    (SELECT unit_of_measure FROM items WHERE item_id = ((s.id % 100) + 1)) as unit_of_measure,
    'Line item ' || ((s.id - 1) % 10) + 1 || ' for RFQ ' || ((s.id - 1) / 10) + 1 as description,
    CURRENT_DATE + (s.id % 60 + 30)::integer as delivery_date,
    (s.id % 500 + 100)::decimal(15,2) as estimated_unit_cost
FROM generate_series(1, 10000) s(id);

-- Insert RFQ suppliers (random assignment)
INSERT INTO rfq_suppliers (rfq_id, supplier_id, invited_date, response_date, is_responded)
SELECT 
    s.rfq_id,
    s.supplier_id,
    CURRENT_TIMESTAMP - (s.id % 30) * INTERVAL '1 day' as invited_date,
    CASE WHEN s.id % 3 = 0 THEN CURRENT_TIMESTAMP - (s.id % 15) * INTERVAL '1 day' ELSE NULL END as response_date,
    s.id % 3 = 0 as is_responded
FROM (
    SELECT 
        r.id as rfq_id,
        sup.id as supplier_id,
        ROW_NUMBER() OVER () as id
    FROM generate_series(1, 1000) r(id)
    CROSS JOIN generate_series(1, 1000) sup(id)
    WHERE (r.id + sup.id) % 7 = 0  -- Random selection
    LIMIT 5001
) s;

-- Insert 10000 quotes
INSERT INTO quotes (rfq_id, supplier_id, line_item_id, quote_number, status, unit_price, total_price, quantity_offered, delivery_date, payment_terms, warranty_period_months, submitted_date, valid_until_date)
SELECT 
    q.rfq_id,
    q.supplier_id,
    q.line_item_id,
    'QTE-' || LPAD(q.id::text, 8, '0') as quote_number,
    (ARRAY['pending', 'submitted', 'awarded', 'rejected'])[(q.id % 4) + 1]::quote_status as status,
    (q.id % 1000 + 50)::decimal(15,2) as unit_price,
    ((q.id % 1000 + 50) * (q.id % 100 + 10))::decimal(15,2) as total_price,
    (q.id % 100 + 10) as quantity_offered,
    CURRENT_DATE + (q.id % 90 + 30)::integer as delivery_date,
    (ARRAY['Net 30', 'Net 45', 'Net 60', '2/10 Net 30'])[(q.id % 4) + 1] as payment_terms,
    (q.id % 36 + 12) as warranty_period_months,
    CURRENT_TIMESTAMP - (q.id % 30) * INTERVAL '1 day' as submitted_date,
    CURRENT_DATE + (q.id % 60 + 30)::integer as valid_until_date
FROM (
    SELECT 
        rli.rfq_id,
        rs.supplier_id,
        rli.line_item_id,
        ROW_NUMBER() OVER () as id
    FROM rfq_line_items rli
    JOIN rfq_suppliers rs ON rli.rfq_id = rs.rfq_id
    WHERE rs.is_responded = true
    ORDER BY random()
    LIMIT 10000
) q;

-- Insert 1000 purchase orders
INSERT INTO purchase_orders (po_number, supplier_id, rfq_id, status, order_date, expected_delivery_date, total_amount, payment_terms, created_by)
SELECT 
    'PO-' || LPAD(s.id::text, 6, '0') as po_number,
    s.supplier_id,
    s.rfq_id,
    (ARRAY['draft', 'sent', 'confirmed', 'received'])[(s.id % 4) + 1]::po_status as status,
    CURRENT_DATE - (s.id % 30)::integer as order_date,
    CURRENT_DATE + (s.id % 60 + 30)::integer as expected_delivery_date,
    (s.id % 50000 + 5000)::decimal(15,2) as total_amount,
    (ARRAY['Net 30', 'Net 45', 'Net 60', '2/10 Net 30'])[(s.id % 4) + 1] as payment_terms,
    'User ' || (s.id % 10 + 1) as created_by
FROM (
    SELECT 
        q.rfq_id,
        q.supplier_id,
        ROW_NUMBER() OVER () as id
    FROM quotes q
    WHERE q.status = 'awarded'
    GROUP BY q.rfq_id, q.supplier_id
    ORDER BY random()
    LIMIT 1000
) s;

-- Insert purchase order lines from awarded quotes
INSERT INTO purchase_order_lines (po_id, quote_id, line_number, item_id, quantity_ordered, unit_price, total_price, delivery_date, description)
SELECT 
    po.po_id,
    q.quote_id,
    ROW_NUMBER() OVER (PARTITION BY po.po_id ORDER BY q.quote_id) as line_number,
    rli.item_id as item_id,
    q.quantity_offered as quantity_ordered,
    q.unit_price,
    q.total_price,
    q.delivery_date,
    'PO Line ' || ROW_NUMBER() OVER (PARTITION BY po.po_id ORDER BY q.quote_id) || ' for PO ' || po.po_number as description
FROM purchase_orders po
JOIN quotes q ON po.rfq_id = q.rfq_id AND po.supplier_id = q.supplier_id
JOIN rfq_line_items rli ON q.line_item_id = rli.line_item_id
WHERE q.status = 'awarded'
LIMIT 10000;

-- Grant permissions (adjust as needed for your use case)
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO postgres;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO postgres; 