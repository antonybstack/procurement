-- Monitoring and Reporting Views
-- This file contains database views for monitoring and reporting purposes

-- Active connections view
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

-- RFQ summary view
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

-- Supplier performance view
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

-- Item supplier history view
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