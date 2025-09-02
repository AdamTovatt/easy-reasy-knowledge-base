using EasyReasy.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Services.Account;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;

namespace EasyReasy.KnowledgeBase.Web.Server.Services.Auth
{
    public class AuthService : IAuthRequestValidationService
    {
        private readonly IServiceProvider _serviceProvider;

        public AuthService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public Task<AuthResponse?> ValidateApiKeyRequestAsync(ApiKeyAuthRequest request, IJwtTokenService jwtTokenService, HttpContext? httpContext = null)
        {
            // API key authentication is not supported
            return Task.FromResult<AuthResponse?>(null);
        }

        public async Task<AuthResponse?> ValidateLoginRequestAsync(LoginAuthRequest request, IJwtTokenService jwtTokenService, HttpContext? httpContext = null)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return null;

            // Get the user service from the HTTP context's service provider (scoped) or fall back to root provider
            IUserService userService = httpContext?.RequestServices.GetRequiredService<IUserService>()
                ?? _serviceProvider.GetRequiredService<IUserService>();

            // Validate credentials using the user service
            User? user = await userService.ValidateCredentialsAsync(request.Username, request.Password);
            if (user == null)
                return null;

            // Update last login timestamp
            await userService.UpdateLastLoginAsync(user.Id);

            // Extract tenant ID from header if available
            string? tenantId = null;
            if (httpContext?.Request.Headers.TryGetValue("X-Tenant-ID", out StringValues headerTenantId) == true)
            {
                tenantId = headerTenantId.ToString();
            }

            // Create JWT token with user information
            DateTime expiresAt = DateTime.UtcNow.AddHours(1);
            string token = jwtTokenService.CreateToken(
                subject: user.Id.ToString(),
                authType: "user",
                additionalClaims: new[]
                {
                    new Claim("tenant_id", tenantId ?? "default"),
                    new Claim("email", user.Email),
                    new Claim("first_name", user.FirstName),
                    new Claim("last_name", user.LastName)
                },
                roles: user.Roles.ToArray(),
                expiresAt: expiresAt);

            return new AuthResponse(token, expiresAt.ToString("o"));
        }
    }
}
