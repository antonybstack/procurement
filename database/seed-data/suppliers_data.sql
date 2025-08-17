-- Supplier Sample Data Generation
-- This file contains the data generation scripts for suppliers and capabilities

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