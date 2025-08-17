-- Database Extensions
-- This file contains all required PostgreSQL extensions for the procurement system

-- UUID generation functions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Performance monitoring and query statistics
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- TimescaleDB for time-series optimization and hypertables
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- Vector similarity search support (standard pgvector)
CREATE EXTENSION IF NOT EXISTS vector;

-- pgvectorscale for enhanced vector performance (TimescaleDB extension)
-- Note: This extension may not be available in all TimescaleDB images
-- Commenting out for initial migration, will be enabled separately
-- CREATE EXTENSION IF NOT EXISTS vectorscale;