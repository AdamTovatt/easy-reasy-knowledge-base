using EasyReasy.Auth;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    public class AuthService : IAuthRequestValidationService
    {
        private readonly IPasswordHasher _passwordHasher;

        public AuthService(IPasswordHasher passwordHasher)
        {
            _passwordHasher = passwordHasher;
        }

        public Task<AuthResponse?> ValidateApiKeyRequestAsync(ApiKeyAuthRequest request, IJwtTokenService jwtTokenService, HttpContext? httpContext = null)
        {
            // API key authentication is not supported
            return Task.FromResult<AuthResponse?>(null);
        }

        public Task<AuthResponse?> ValidateLoginRequestAsync(LoginAuthRequest request, IJwtTokenService jwtTokenService, HttpContext? httpContext = null)
        {
            // For now, we'll use a simple username/password validation
            // In a real application, you would check against a database
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Task.FromResult<AuthResponse?>(null);

            // Simple validation - you should replace this with proper user lookup
            // For demo purposes, accept any non-empty username/password
            if (request.Username.Length < 3 || request.Password.Length < 3)
                return Task.FromResult<AuthResponse?>(null);

            // Extract tenant ID from header if available
            string? tenantId = null;
            if (httpContext?.Request.Headers.TryGetValue("X-Tenant-ID", out StringValues headerTenantId) == true)
            {
                tenantId = headerTenantId.ToString();
            }

            // Create JWT token
            DateTime expiresAt = DateTime.UtcNow.AddHours(1);
            string token = jwtTokenService.CreateToken(
                subject: request.Username,
                authType: "user",
                additionalClaims: new[] { new Claim("tenant_id", tenantId ?? "default") },
                roles: new[] { "user" },
                expiresAt: expiresAt);

            return Task.FromResult<AuthResponse?>(new AuthResponse(token, expiresAt.ToString("o")));
        }
    }
}
