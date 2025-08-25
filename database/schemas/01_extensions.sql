-- Database Extensions

-- UUID generation functions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Performance monitoring and query statistics
CREATE EXTENSION IF NOT EXISTS "pg_stat_statements";

-- TimescaleDB for time-series optimization and hypertables
CREATE EXTENSION IF NOT EXISTS timescaledb;

-- pgvectorscale for enhanced vector performance (TimescaleDB extension)
CREATE EXTENSION IF NOT EXISTS vectorscale CASCADE;
-- CASCADE implicitly installs Vector similarity search support (standard pgvector)
-- CREATE EXTENSION IF NOT EXISTS vector;