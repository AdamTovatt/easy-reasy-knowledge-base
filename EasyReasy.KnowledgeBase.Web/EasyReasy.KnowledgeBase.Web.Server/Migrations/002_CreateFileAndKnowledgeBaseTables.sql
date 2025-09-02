-- Migration: 002_CreateFileAndKnowledgeBaseTables
-- Description: Creates library and file tables with proper foreign key relationships and permission system

-- Create enum for library permission types
CREATE TYPE library_permission_type AS ENUM ('read', 'write', 'admin');

-- Create library table
CREATE TABLE IF NOT EXISTS library (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) UNIQUE NOT NULL,       -- Globally unique library name
    description VARCHAR(1000),               -- Optional description with reasonable length limit
    owner_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Owner user (renamed from created_by_user_id)
    is_public BOOLEAN NOT NULL DEFAULT false, -- Whether library is publicly readable
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP
);

-- Create library_permission table for explicit permission grants
CREATE TABLE IF NOT EXISTS library_permission (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    library_id UUID NOT NULL REFERENCES library(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
    permission_type library_permission_type NOT NULL,
    granted_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    UNIQUE(library_id, user_id)  -- One permission record per user per library
);

-- Create library_file table
CREATE TABLE IF NOT EXISTS library_file (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    library_id UUID NOT NULL REFERENCES library(id) ON DELETE RESTRICT,  -- Foreign key to library
    original_file_name VARCHAR(500) NOT NULL,  -- Original filename with reasonable length limit
    content_type VARCHAR(255) NOT NULL,        -- MIME type (e.g., 'application/pdf', 'text/plain')
    size_in_bytes BIGINT NOT NULL CHECK (size_in_bytes >= 0),  -- File size with non-negative constraint
    relative_path VARCHAR(1000) NOT NULL,      -- Relative path within file storage system
    file_hash BYTEA NOT NULL,                   -- SHA-256 hash of the file content for deduplication and integrity
    uploaded_by_user_id UUID NOT NULL REFERENCES "user"(id) ON DELETE RESTRICT,  -- Preserve files when user deleted
    uploaded_at TIMESTAMPTZ NOT NULL,          -- When the file was uploaded (can differ from created_at)
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    
    -- Ensure unique relative paths within the same library
    UNIQUE(library_id, relative_path)
);

-- Create indexes for library table
CREATE INDEX IF NOT EXISTS idx_library_owner_id ON library(owner_id);
CREATE INDEX IF NOT EXISTS idx_library_name ON library(name);
CREATE INDEX IF NOT EXISTS idx_library_is_public ON library(is_public);

-- Create indexes for library_permission table  
CREATE INDEX IF NOT EXISTS idx_library_permission_library_id ON library_permission(library_id);
CREATE INDEX IF NOT EXISTS idx_library_permission_user_id ON library_permission(user_id);
CREATE INDEX IF NOT EXISTS idx_library_permission_type ON library_permission(permission_type);

-- Create indexes for better performance on library_file table
CREATE INDEX IF NOT EXISTS idx_library_file_library_id ON library_file(library_id);
CREATE INDEX IF NOT EXISTS idx_library_file_uploaded_by_user_id ON library_file(uploaded_by_user_id);
CREATE INDEX IF NOT EXISTS idx_library_file_uploaded_at ON library_file(uploaded_at DESC);  -- For sorting by upload time
CREATE INDEX IF NOT EXISTS idx_library_file_content_type ON library_file(content_type);     -- For filtering by file type
CREATE INDEX IF NOT EXISTS idx_library_file_original_file_name ON library_file(original_file_name); -- For searching by filename
CREATE INDEX IF NOT EXISTS idx_library_file_hash ON library_file(file_hash);                -- For deduplication and integrity checks

-- Create triggers to automatically update updated_at (reuses existing function)
CREATE TRIGGER update_library_updated_at BEFORE UPDATE ON library
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_library_file_updated_at BEFORE UPDATE ON library_file
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();