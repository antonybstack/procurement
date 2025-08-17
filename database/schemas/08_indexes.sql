-- Database Indexes for Performance
-- This file contains all performance indexes for the procurement system

-- Basic indexes for search and filtering
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
-- Note: Only suppliers will use advanced diskann indexes after TigerData migration
CREATE INDEX idx_suppliers_embedding ON suppliers USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_items_embedding ON items USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_rfqs_embedding ON request_for_quotes USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_rfq_line_items_embedding ON rfq_line_items USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_quotes_embedding ON quotes USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_purchase_orders_embedding ON purchase_orders USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);
CREATE INDEX idx_purchase_order_lines_embedding ON purchase_order_lines USING ivfflat (embedding vector_cosine_ops) WITH (lists = 100);