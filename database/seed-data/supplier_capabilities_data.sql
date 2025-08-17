-- Supplier Capabilities Sample Data Generation
-- This file generates meaningful supplier capabilities for AI-driven recommendations

-- Clear existing capabilities to ensure a fresh start
DELETE FROM supplier_capabilities;

-- Cluster 1: Aerospace & Defense Specialists (Suppliers 1-200)
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
INSERT INTO supplier_capabilities (supplier_id, capability_type, capability_value)
SELECT
    s.id,
    'Service',
    (ARRAY['Anodizing', 'Heat Treatment', 'Non-Destructive Testing'])[ ((s.id + FLOOR(random() * 3))::integer) % 3 + 1 ]
FROM generate_series(801, 1000) s(id);