-- Migration: 002_CreateFileAndKnowledgeBaseTables
-- Description: Creates knowledge base and file tables with proper foreign key relationships and permission system

-- Create enum for knowledge base permission types
CREATE TYPE knowledge_base_permission_type AS ENUM ('read', 'write', 'admin');

-- Create knowledge_base table
CREATE TABLE IF NOT EXISTS knowledge_base (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) UNIQUE NOT NULL,       -- Globally unique knowledge base name
    description VARCHAR(1000),               -- Optional description with reasonable length limit
    owner_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Owner user (renamed from created_by_user_id)
    is_public BOOLEAN NOT NULL DEFAULT false, -- Whether knowledge base is publicly readable
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Create knowledge_base_permission table for explicit permission grants
CREATE TABLE IF NOT EXISTS knowledge_base_permission (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    knowledge_base_id UUID NOT NULL REFERENCES knowledge_base(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    permission_type knowledge_base_permission_type NOT NULL,
    granted_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(knowledge_base_id, user_id)  -- One permission record per user per KB
);

-- Create knowledge_file table
CREATE TABLE IF NOT EXISTS knowledge_file (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    knowledge_base_id UUID NOT NULL REFERENCES knowledge_base(id) ON DELETE RESTRICT,  -- Foreign key to knowledge base
    original_file_name VARCHAR(500) NOT NULL,  -- Original filename with reasonable length limit
    content_type VARCHAR(255) NOT NULL,        -- MIME type (e.g., 'application/pdf', 'text/plain')
    size_in_bytes BIGINT NOT NULL CHECK (size_in_bytes >= 0),  -- File size with non-negative constraint
    relative_path VARCHAR(1000) NOT NULL,      -- Relative path within file storage system
    file_hash BYTEA NOT NULL,                   -- SHA-256 hash of the file content for deduplication and integrity
    uploaded_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Preserve files when user deleted
    uploaded_at TIMESTAMPTZ NOT NULL,          -- When the file was uploaded (can differ from created_at)
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- Ensure unique relative paths within the same knowledge base
    UNIQUE(knowledge_base_id, relative_path)
);

-- Create indexes for knowledge_base table
CREATE INDEX IF NOT EXISTS idx_knowledge_base_owner_id ON knowledge_base(owner_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_base_name ON knowledge_base(name);
CREATE INDEX IF NOT EXISTS idx_knowledge_base_is_public ON knowledge_base(is_public);

-- Create indexes for knowledge_base_permission table  
CREATE INDEX IF NOT EXISTS idx_knowledge_base_permission_kb_id ON knowledge_base_permission(knowledge_base_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_base_permission_user_id ON knowledge_base_permission(user_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_base_permission_type ON knowledge_base_permission(permission_type);

-- Create indexes for better performance on knowledge_file table
CREATE INDEX IF NOT EXISTS idx_knowledge_file_knowledge_base_id ON knowledge_file(knowledge_base_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_file_uploaded_by_user_id ON knowledge_file(uploaded_by_user_id);
CREATE INDEX IF NOT EXISTS idx_knowledge_file_uploaded_at ON knowledge_file(uploaded_at DESC);  -- For sorting by upload time
CREATE INDEX IF NOT EXISTS idx_knowledge_file_content_type ON knowledge_file(content_type);     -- For filtering by file type
CREATE INDEX IF NOT EXISTS idx_knowledge_file_original_file_name ON knowledge_file(original_file_name); -- For searching by filename
CREATE INDEX IF NOT EXISTS idx_knowledge_file_hash ON knowledge_file(file_hash);                -- For deduplication and integrity checks

-- Create triggers to automatically update updated_at (reuses existing function)
CREATE TRIGGER update_knowledge_base_updated_at BEFORE UPDATE ON knowledge_base
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_knowledge_file_updated_at BEFORE UPDATE ON knowledge_file
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();