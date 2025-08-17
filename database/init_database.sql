-- Main Database Initialization Script
-- This script orchestrates the creation of the complete procurement database

-- Load all schema files in order
\i database/schemas/01_extensions.sql
\i database/schemas/02_enums.sql
\i database/schemas/03_suppliers.sql
\i database/schemas/04_items.sql
\i database/schemas/05_rfqs.sql
\i database/schemas/06_quotes.sql
\i database/schemas/07_purchase_orders.sql
\i database/schemas/08_indexes.sql
\i database/schemas/09_functions_triggers.sql

-- Load views
\i database/views/monitoring_views.sql

-- Load seed data
\i database/seed-data/suppliers_data.sql
\i database/seed-data/supplier_capabilities_data.sql
\i database/seed-data/other_data.sql