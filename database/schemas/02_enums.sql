-- Enum Type Definitions
-- This file contains all custom enum types used throughout the procurement system

-- Quote status enumeration
CREATE TYPE quote_status AS ENUM ('pending', 'submitted', 'awarded', 'rejected', 'expired');

-- Request for Quote status enumeration  
CREATE TYPE rfq_status AS ENUM ('draft', 'published', 'closed', 'awarded', 'cancelled');

-- Purchase Order status enumeration
CREATE TYPE po_status AS ENUM ('draft', 'sent', 'confirmed', 'received', 'cancelled');

-- Item category enumeration
CREATE TYPE item_category AS ENUM ('electronics', 'machinery', 'raw_materials', 'packaging', 'services', 'components');