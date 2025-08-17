# Database Structure

This directory contains the modular database schema for the Procurement Management System, organized for maintainability and clarity.

## Directory Structure

```
database/
├── init_database.sql          # Main initialization script
├── schemas/                   # Table definitions and core schema
│   ├── 01_extensions.sql      # PostgreSQL extensions
│   ├── 02_enums.sql          # Custom enum types
│   ├── 03_suppliers.sql      # Suppliers table and related
│   ├── 04_items.sql          # Items table and specifications
│   ├── 05_rfqs.sql           # RFQ tables and line items
│   ├── 06_quotes.sql         # Quotes table
│   ├── 07_purchase_orders.sql # Purchase orders and lines
│   ├── 08_indexes.sql        # Performance indexes
│   └── 09_functions_triggers.sql # Functions and triggers
├── views/                    # Database views
│   └── monitoring_views.sql  # Monitoring and reporting views
├── seed-data/               # Sample data generation
│   ├── suppliers_data.sql   # Supplier sample data
│   ├── supplier_capabilities_data.sql # Supplier capabilities
│   └── other_data.sql       # Items, RFQs, quotes, POs
└── migrations/              # Future migration scripts
```

## Usage

The database is automatically initialized when Docker containers start via the `init_database.sql` script, which loads all components in the correct order.

## Vector Search Focus

The current structure supports vector embeddings on all tables, but the TigerData migration plan focuses specifically on enhancing the **suppliers table** with:

- Advanced diskann indexes
- Label-based filtering
- Automated embedding generation via pgai

Other tables will continue using standard pgvector indexes until future migrations.

## Maintenance

- Add new tables to the appropriate schema file
- Update `init_database.sql` to include new files
- Use the `migrations/` directory for schema changes
- Keep seed data separate from production schema