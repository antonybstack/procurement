-- Sample Data for Non-Supplier Tables
-- This file contains data generation for items, RFQs, quotes, and purchase orders

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