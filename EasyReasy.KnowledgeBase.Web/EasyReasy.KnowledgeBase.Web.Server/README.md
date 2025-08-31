# EasyReasy Knowledge Base Web Server

This is the web server component of the EasyReasy Knowledge Base system, providing a REST API for chat functionality with authentication.

## Authentication

The server uses EasyReasy.Auth for JWT-based authentication. Email/password authentication is supported.

### Email/Password Authentication
**Endpoint:** `POST /api/auth/login`

**Request Body:**
```json
{
  "username": "your-email@example.com",
  "password": "your-password"
}
```

### Response Format
The authentication endpoint returns:
```json
{
  "token": "jwt-token-here",
  "expiresAt": "2024-01-01T12:00:00.000Z"
}
```

## Using Authenticated Endpoints

After obtaining a JWT token, include it in the Authorization header for all protected endpoints:

```
Authorization: Bearer your-jwt-token-here
```

### Example: Chat Stream Endpoint
**Endpoint:** `POST /api/chat/stream`

**Headers:**
```
Authorization: Bearer your-jwt-token-here
Content-Type: application/json
```

**Request Body:**
```json
{
  "message": "Hello, how can you help me?"
}
```

## Environment Variables

Make sure to set the following environment variable:

- `JWT_SIGNING_SECRET`: A secure random string at least 32 characters long (used for signing JWT tokens)

## Security Features

- **Progressive Delay**: Built-in protection against brute-force attacks
- **JWT Tokens**: Secure token-based authentication
- **Tenant Support**: Multi-tenant support via `X-Tenant-ID` header
- **Role-Based Access**: User roles are included in JWT tokens

## Development Notes

The current authentication implementation uses a simple validation approach for demonstration purposes. In production, you should:

1. Replace the simple validation in `AuthService.cs` with proper database lookups
2. Implement proper user management
3. Add role-based authorization checks
4. Consider implementing refresh tokens for longer sessions
