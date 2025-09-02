-- Migration: 002_CreateFileAndKnowledgeBaseTables
-- Description: Creates knowledge base and file tables with proper foreign key relationships

-- Create knowledge_base table
CREATE TABLE IF NOT EXISTS knowledge_base (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) UNIQUE NOT NULL,       -- Globally unique knowledge base name
    description VARCHAR(1000),               -- Optional description with reasonable length limit
    created_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Creator user
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Create knowledge_file table
CREATE TABLE IF NOT EXISTS knowledge_file (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    knowledge_base_id UUID NOT NULL REFERENCES knowledge_base(id) ON DELETE RESTRICT,  -- Foreign key to knowledge base
    original_file_name VARCHAR(500) NOT NULL,  -- Original filename with reasonable length limit
    content_type VARCHAR(255) NOT NULL,        -- MIME type (e.g., 'application/pdf', 'text/plain')
    size_in_bytes BIGINT NOT NULL CHECK (size_in_bytes >= 0),  -- File size with non-negative constraint
    relative_path VARCHAR(1000) NOT NULL,      -- Relative path within file storage system
    uploaded_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Preserve files when user deleted
    uploaded_at TIMESTAMPTZ NOT NULL,          -- When the file was uploaded (can differ from created_at)
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- Ensure unique relative paths within the same knowledge base
    UNIQUE(knowledge_base_id, relative_path)
);

-- Create indexes for better performance on knowledge_base table
CREATE INDEX IF NOT EXISTS idx_knowledge_base_created_by_user_id ON knowledge_base(created_by_user_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_base_name ON knowledge_base(name);

-- Create indexes for better performance on knowledge_file table
CREATE INDEX IF NOT EXISTS idx_knowledge_file_knowledge_base_id ON knowledge_file(knowledge_base_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_file_uploaded_by_user_id ON knowledge_file(uploaded_by_user_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_file_uploaded_at ON knowledge_file(uploaded_at DESC);  -- For sorting by upload time
CREATE INDEX IF NOT EXISTS idx_knowledge_file_content_type ON knowledge_file(content_type);     -- For filtering by file type
CREATE INDEX IF NOT EXISTS idx_knowledge_file_original_file_name ON knowledge_file(original_file_name); -- For searching by filename

-- Create triggers to automatically update updated_at (reuses existing function)
CREATE TRIGGER update_knowledge_base_updated_at BEFORE UPDATE ON knowledge_base
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_knowledge_file_updated_at BEFORE UPDATE ON knowledge_file
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();