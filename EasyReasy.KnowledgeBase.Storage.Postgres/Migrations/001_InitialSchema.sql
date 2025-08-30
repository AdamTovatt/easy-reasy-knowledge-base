-- Migration: 001_InitialSchema
-- Description: Initial schema for EasyReasy KnowledgeBase
-- Tables: knowledge_file, knowledge_section, knowledge_chunk

-- Create knowledge_file table
CREATE TABLE IF NOT EXISTS knowledge_file (
    id UUID PRIMARY KEY,
    name TEXT NOT NULL,
    hash BYTEA NOT NULL,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    status INTEGER NOT NULL
);

-- Create knowledge_section table
CREATE TABLE IF NOT EXISTS knowledge_section (
    id UUID PRIMARY KEY,
    file_id UUID NOT NULL,
    section_index INTEGER NOT NULL,
    summary TEXT,
    additional_context TEXT,
    UNIQUE (file_id, section_index)
);

-- Create knowledge_chunk table
CREATE TABLE IF NOT EXISTS knowledge_chunk (
    id UUID PRIMARY KEY,
    section_id UUID NOT NULL,
    chunk_index INTEGER NOT NULL,
    content TEXT NOT NULL,
    embedding BYTEA,
    file_id UUID NOT NULL,
    UNIQUE (section_id, chunk_index)
);

-- Create indexes for better performance
CREATE INDEX IF NOT EXISTS idx_chunks_section_id ON knowledge_chunk (section_id);
CREATE INDEX IF NOT EXISTS idx_chunks_file_id ON knowledge_chunk (file_id);
CREATE INDEX IF NOT EXISTS idx_chunks_section_index ON knowledge_chunk (section_id, chunk_index);
CREATE INDEX IF NOT EXISTS idx_sections_file_id ON knowledge_section (file_id);
CREATE INDEX IF NOT EXISTS idx_sections_file_index ON knowledge_section (file_id, section_index);
