-- Migration: 001_InitialSchema
-- Description: Initial schema for EasyReasy KnowledgeBase
-- Tables: knowledge_files, knowledge_sections, knowledge_chunks

-- Create knowledge_files table
CREATE TABLE IF NOT EXISTS knowledge_files (
    id UUID PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    hash BYTEA NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    status INTEGER NOT NULL
);

-- Create knowledge_sections table
CREATE TABLE IF NOT EXISTS knowledge_sections (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    section_index INTEGER NOT NULL,
    summary TEXT,
    additional_context TEXT
);

-- Create knowledge_chunks table
CREATE TABLE IF NOT EXISTS knowledge_chunks (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding BYTEA,
    file_id UUID NOT NULL
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_chunks_section_id ON knowledge_chunks (section_id);
CREATE INDEX IF NOT EXISTS idx_chunks_file_id ON knowledge_chunks (file_id);
CREATE INDEX IF NOT EXISTS idx_chunks_section_index ON knowledge_chunks (section_id, chunk_index);
CREATE INDEX IF NOT EXISTS idx_sections_file_id ON knowledge_sections (file_id);
CREATE INDEX IF NOT EXISTS idx_sections_file_index ON knowledge_sections (file_id, section_index);
