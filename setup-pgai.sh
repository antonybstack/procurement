#!/bin/bash

# Setup script for pgai installation and configuration
# This script must be run after the database is up and running

set -e

echo "🚀 Setting up pgai for TigerData migration..."

# Check if database is running
echo "📊 Checking database connection..."
if ! docker-compose exec postgres pg_isready -U postgres -d myapp; then
    echo "❌ Database is not ready. Please start the database first with: docker-compose up -d postgres"
    exit 1
fi

# Install pgai in a Python virtual environment
echo "🐍 Setting up Python environment for pgai..."
python3 -m venv venv_pgai
source venv_pgai/bin/activate

echo "📦 Installing pgai..."
pip install pgai

# Configure pgai with the database
echo "🔧 Configuring pgai with database..."
export OPENAI_API_KEY="${OpenAIKey}"
if [ -z "$OPENAI_API_KEY" ]; then
    echo "⚠️  Warning: OpenAI API key not found. Please set OpenAIKey environment variable."
    echo "   You can set it in config.env or export it manually:"
    echo "   export OpenAIKey='your-api-key-here'"
    read -p "Press Enter to continue with pgai installation (you can configure API key later)..."
fi

# Install pgai in the database
echo "💾 Installing pgai schema in database..."
pgai install -d "postgresql://postgres:postgres_password@localhost:5432/myapp"

echo "✅ pgai installation completed!"
echo ""
echo "Next steps:"
echo "1. Run the database migrations: docker-compose exec postgres psql -U postgres -d myapp -f /docker-entrypoint-initdb.d/database/migrations/003_setup_pgai.sql"
echo "2. Ensure OpenAI API key is configured"
echo "3. Test supplier vectorization"

deactivate