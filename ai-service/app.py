#!/usr/bin/env python3
"""
Simple AI Recommendation Service using SQLCoder
This is a lightweight wrapper around SQLCoder for supplier recommendations
"""

from flask import Flask, request, jsonify
import sqlite3
import json
import os
import logging
from datetime import datetime
import subprocess
import tempfile

app = Flask(__name__)

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Configuration
MODEL_NAME = os.getenv('MODEL_NAME', 'defog/sqlcoder-7b')
MAX_TOKENS = int(os.getenv('MAX_TOKENS', '512'))
TEMPERATURE = float(os.getenv('TEMPERATURE', '0.1'))
TOP_P = float(os.getenv('TOP_P', '0.95'))
DATABASE_URL = os.getenv('DATABASE_URL', 'postgresql://postgres:postgres_password@postgres:5432/myapp')

# Schema file path
SCHEMA_FILE = '/app/schema.sql'

@app.route('/health', methods=['GET'])
def health_check():
    """Health check endpoint"""
    try:
        # Try to connect to database to verify connectivity
        import psycopg2
        conn = psycopg2.connect(DATABASE_URL)
        conn.close()
        db_status = "connected"
    except Exception as e:
        db_status = f"disconnected: {str(e)}"
    
    return jsonify({
        'status': 'healthy',
        'service': 'ai-recommendation-service',
        'timestamp': datetime.utcnow().isoformat(),
        'model': MODEL_NAME,
        'database': db_status
    })

@app.route('/generate', methods=['POST'])
def generate_sql():
    """Generate SQL query using SQLCoder"""
    try:
        data = request.get_json()
        prompt = data.get('prompt', '')
        max_tokens = data.get('max_tokens', MAX_TOKENS)
        temperature = data.get('temperature', TEMPERATURE)
        top_p = data.get('top_p', TOP_P)

        if not prompt:
            return jsonify({'error': 'Prompt is required'}), 400

        logger.info(f"Generating SQL for prompt: {prompt[:100]}...")

        # For POC, we'll return predefined SQL queries based on the prompt
        # In production, this would call the actual SQLCoder model
        sql_query = generate_mock_sql(prompt)

        return jsonify({
            'sql': sql_query,
            'explanation': 'Generated SQL query for supplier recommendations',
            'model': MODEL_NAME,
            'timestamp': datetime.utcnow().isoformat()
        })

    except Exception as e:
        logger.error(f"Error generating SQL: {str(e)}")
        return jsonify({'error': str(e)}), 500

def generate_mock_sql(prompt):
    """Generate mock SQL queries for demonstration"""
    
    # Check if the prompt is about supplier recommendations
    if 'supplier' in prompt.lower() and 'recommendation' in prompt.lower():
        if 'item code' in prompt.lower() or 'part number' in prompt.lower():
            return """
            SELECT 
                s.supplier_id,
                s.supplier_code,
                s.company_name,
                s.country,
                s.rating,
                COALESCE(AVG(q.unit_price), 0) as avg_price,
                COUNT(DISTINCT q.quote_id) as quote_count,
                COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) as awarded_count,
                CASE 
                    WHEN COUNT(DISTINCT q.quote_id) > 0 
                    THEN ROUND((COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) * 100.0 / COUNT(DISTINCT q.quote_id)), 2)
                    ELSE 0 
                END as success_rate,
                CASE 
                    WHEN s.rating >= 4 THEN 'High rating supplier with excellent track record'
                    WHEN s.rating >= 3 THEN 'Good supplier with solid performance'
                    ELSE 'Supplier with room for improvement'
                END as reasoning
            FROM suppliers s
            LEFT JOIN quotes q ON s.supplier_id = q.supplier_id
            LEFT JOIN rfq_line_items rli ON q.line_item_id = rli.line_item_id
            LEFT JOIN items i ON rli.item_id = i.item_id
            WHERE s.is_active = true
            AND (i.item_code = 'ITEM001' OR i.category = 'electronics')
            GROUP BY s.supplier_id, s.supplier_code, s.company_name, s.country, s.rating
            HAVING COUNT(DISTINCT q.quote_id) > 0
            ORDER BY s.rating DESC, success_rate DESC, avg_price ASC
            LIMIT 10;
            """
        else:
            return """
            SELECT 
                s.supplier_id,
                s.supplier_code,
                s.company_name,
                s.country,
                s.rating,
                COALESCE(AVG(q.unit_price), 0) as avg_price,
                COUNT(DISTINCT q.quote_id) as quote_count,
                COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) as awarded_count,
                CASE 
                    WHEN COUNT(DISTINCT q.quote_id) > 0 
                    THEN ROUND((COUNT(DISTINCT CASE WHEN q.status = 'awarded' THEN q.quote_id END) * 100.0 / COUNT(DISTINCT q.quote_id)), 2)
                    ELSE 0 
                END as success_rate,
                'Supplier with good performance in similar categories' as reasoning
            FROM suppliers s
            LEFT JOIN quotes q ON s.supplier_id = q.supplier_id
            WHERE s.is_active = true
            GROUP BY s.supplier_id, s.supplier_code, s.company_name, s.country, s.rating
            HAVING COUNT(DISTINCT q.quote_id) > 0
            ORDER BY s.rating DESC, success_rate DESC
            LIMIT 10;
            """
    
    # Check if the prompt is about performance analysis
    elif 'performance' in prompt.lower() and 'analysis' in prompt.lower():
        return """
        SELECT 
            i.item_code,
            i.description,
            COUNT(DISTINCT s.supplier_id) as total_suppliers,
            COUNT(DISTINCT CASE WHEN s.is_active = true THEN s.supplier_id END) as active_suppliers,
            AVG(q.unit_price) as average_price,
            MIN(q.unit_price) as min_price,
            MAX(q.unit_price) as max_price,
            STDDEV(q.unit_price) as price_variance
        FROM items i
        LEFT JOIN rfq_line_items rli ON i.item_id = rli.item_id
        LEFT JOIN quotes q ON rli.line_item_id = q.line_item_id
        LEFT JOIN suppliers s ON q.supplier_id = s.supplier_id
        WHERE i.item_code = 'ITEM001'
        GROUP BY i.item_id, i.item_code, i.description;
        """
    
    # Default query
    return """
    SELECT 
        s.supplier_id,
        s.supplier_code,
        s.company_name,
        s.country,
        s.rating
    FROM suppliers s
    WHERE s.is_active = true
    ORDER BY s.rating DESC
    LIMIT 10;
    """

@app.route('/schema', methods=['GET'])
def get_schema():
    """Get database schema"""
    try:
        if os.path.exists(SCHEMA_FILE):
            with open(SCHEMA_FILE, 'r') as f:
                schema = f.read()
            return jsonify({
                'schema': schema,
                'timestamp': datetime.utcnow().isoformat()
            })
        else:
            return jsonify({'error': 'Schema file not found'}), 404
    except Exception as e:
        logger.error(f"Error reading schema: {str(e)}")
        return jsonify({'error': str(e)}), 500

@app.route('/models', methods=['GET'])
def list_models():
    """List available models"""
    return jsonify({
        'models': [
            {
                'name': 'defog/sqlcoder-7b',
                'description': 'SQLCoder 7B model for SQL generation',
                'parameters': '7B'
            },
            {
                'name': 'defog/sqlcoder-3b',
                'description': 'SQLCoder 3B model for SQL generation (faster)',
                'parameters': '3B'
            }
        ],
        'current_model': MODEL_NAME,
        'timestamp': datetime.utcnow().isoformat()
    })

if __name__ == '__main__':
    logger.info(f"Starting AI Recommendation Service with model: {MODEL_NAME}")
    logger.info(f"Database URL: {DATABASE_URL}")
    
    app.run(host='0.0.0.0', port=8000, debug=False) 