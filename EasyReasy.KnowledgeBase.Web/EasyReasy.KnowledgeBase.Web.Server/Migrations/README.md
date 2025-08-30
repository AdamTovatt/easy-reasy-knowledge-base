# Database Migrations

This project uses DbUp to manage PostgreSQL database migrations.

## How It Works

- Migrations are SQL scripts stored in the `Migrations/` folder
- Scripts are automatically embedded as resources in the assembly
- DbUp tracks which migrations have been applied in a `SchemaVersions` table
- Migrations run automatically when the application starts

## Adding New Migrations

1. Create a new SQL file in the `Migrations/` folder
2. Use the naming convention: `XXX_Description.sql` (e.g., `002_AddUserPreferenceTable.sql`)
3. Write your PostgreSQL SQL script
4. The migration will run automatically on the next application start

## Migration Script Guidelines

- Use `IF NOT EXISTS` for CREATE statements to make scripts idempotent
- Use `CREATE OR REPLACE` for functions and procedures
- Always include a comment header with migration number and description
- Test migrations on a copy of your database before applying to production
- Use singular table names (e.g., `user` not `users`)

## Example Migration Script

```sql
-- Migration: 002_AddUserPreferenceTable
-- Description: Creates user_preference table with UUID primary keys for storing user-specific settings

CREATE TABLE IF NOT EXISTS user_preference (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES user(id) ON DELETE CASCADE,
    preference_key VARCHAR(100) NOT NULL,
    preference_value TEXT,
    created_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(user_id, preference_key)
);

CREATE INDEX IF NOT EXISTS idx_user_preference_user_id ON user_preference(user_id);
```

## Configuration

Set the `POSTGRES_CONNECTION_STRING` environment variable to point to your PostgreSQL database:

```
POSTGRES_CONNECTION_STRING=Host=localhost;Database=knowledgebasedb;Username=postgres;Password=your-password
```

## Troubleshooting

- Check the application logs for migration errors
- Ensure the database user has sufficient permissions
- Verify the connection string is correct
- If migrations fail, the application will exit with an error message
