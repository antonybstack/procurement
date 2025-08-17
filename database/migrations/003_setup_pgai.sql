-- Migration: Setup pgai for Automated Embedding Generation
-- This script configures pgai for automated supplier embedding generation

-- Note: pgai must be installed via pip in the application environment first
-- pip install pgai
-- pgai install -d "postgresql://postgres:postgres_password@localhost:5432/myapp"

-- The pgai installation creates an 'ai' schema automatically
-- This script configures vectorizers for the suppliers table

-- Create a vectorizer for suppliers that will automatically generate embeddings
-- This replaces manual embedding generation in the application layer
SELECT ai.create_vectorizer(
    'public.suppliers',
    destination => 'embedding',
    embedding => ai.embedding_openai('text-embedding-3-small', 768),
    chunking => ai.chunking_character_text_splitter(1000),
    formatting => ai.formatting_python_template(
        'Company: {{company_name}}
Contact: {{contact_name}}
Location: {{city}}, {{state}}, {{country}}
Rating: {{rating}}/5
Payment Terms: {{payment_terms}}
Certifications: {{certification_labels}}
Processes: {{process_labels}}
Materials: {{material_labels}}
Services: {{service_labels}}'
    ),
    processing => ai.processing_default()
);

-- Create a schedule to process vectorization regularly
-- This ensures new suppliers get embeddings automatically
SELECT ai.schedule_vectorizer(
    'public.suppliers',
    schedule => 'every 5 minutes'
);

-- Enable vectorizer (starts the background processing)
SELECT ai.enable_vectorizer('public.suppliers');