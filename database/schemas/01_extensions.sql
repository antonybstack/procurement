-- Database Extensions
-- This file contains all required PostgreSQL extensions for the procurement system

-- UUID generation functions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Performance monitoring and query statistics
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- Vector similarity search support
CREATE EXTENSION IF NOT EXISTS vector;