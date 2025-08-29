-- Migration: 001_InitialSchema
-- Description: Initial database schema for EasyReasy Knowledge Base

-- Create users table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) UNIQUE NOT NULL,
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Create knowledge_files table
CREATE TABLE IF NOT EXISTS knowledge_files (
    id SERIAL PRIMARY KEY,
    file_name VARCHAR(500) NOT NULL,
    file_path VARCHAR(1000) NOT NULL,
    file_size BIGINT NOT NULL,
    file_hash VARCHAR(64) NOT NULL,
    content_type VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(file_path, file_hash)
);

-- Create knowledge_sections table
CREATE TABLE IF NOT EXISTS knowledge_sections (
    id SERIAL PRIMARY KEY,
    knowledge_file_id INTEGER NOT NULL REFERENCES knowledge_files(id) ON DELETE CASCADE,
    section_name VARCHAR(255),
    section_content TEXT NOT NULL,
    section_order INTEGER NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(knowledge_file_id, section_order)
);

-- Create knowledge_chunks table
CREATE TABLE IF NOT EXISTS knowledge_chunks (
    id SERIAL PRIMARY KEY,
    knowledge_section_id INTEGER NOT NULL REFERENCES knowledge_sections(id) ON DELETE CASCADE,
    chunk_content TEXT NOT NULL,
    chunk_order INTEGER NOT NULL,
    embedding_vector REAL[],
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(knowledge_section_id, chunk_order)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_knowledge_files_path ON knowledge_files(file_path);
CREATE INDEX IF NOT EXISTS idx_knowledge_files_hash ON knowledge_files(file_hash);
CREATE INDEX IF NOT EXISTS idx_knowledge_sections_file_id ON knowledge_sections(knowledge_file_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_chunks_section_id ON knowledge_chunks(knowledge_section_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_chunks_embedding ON knowledge_chunks USING GIN(embedding_vector);

-- Create function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Create triggers to automatically update updated_at
CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_knowledge_files_updated_at BEFORE UPDATE ON knowledge_files
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();
