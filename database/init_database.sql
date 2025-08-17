-- Main Database Initialization Script
-- This script orchestrates the creation of the complete procurement database

-- Load all schema files in order
\i /docker-entrypoint-initdb.d/database/schemas/01_extensions.sql
\i /docker-entrypoint-initdb.d/database/schemas/02_enums.sql
\i /docker-entrypoint-initdb.d/database/schemas/03_suppliers.sql
\i /docker-entrypoint-initdb.d/database/schemas/04_items.sql
\i /docker-entrypoint-initdb.d/database/schemas/05_rfqs.sql
\i /docker-entrypoint-initdb.d/database/schemas/06_quotes.sql
\i /docker-entrypoint-initdb.d/database/schemas/07_purchase_orders.sql
\i /docker-entrypoint-initdb.d/database/schemas/08_indexes.sql
\i /docker-entrypoint-initdb.d/database/schemas/09_functions_triggers.sql

-- Load views
\i /docker-entrypoint-initdb.d/database/views/monitoring_views.sql

-- Load seed data
\i /docker-entrypoint-initdb.d/database/seed-data/suppliers_data.sql
\i /docker-entrypoint-initdb.d/database/seed-data/supplier_capabilities_data.sql
\i /docker-entrypoint-initdb.d/database/seed-data/other_data.sql