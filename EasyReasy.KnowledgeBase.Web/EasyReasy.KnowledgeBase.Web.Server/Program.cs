using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using EasyReasy.KnowledgeBase.Web.Server.Services;
using EasyReasy.Auth;
using Microsoft.AspNetCore.Http.Json;

namespace EasyReasy.KnowledgeBase.Web.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(EnvironmentVariables));

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Configure JSON serialization to use camelCase
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonConfiguration.DefaultOptions.PropertyNamingPolicy;
                options.SerializerOptions.PropertyNameCaseInsensitive = JsonConfiguration.DefaultOptions.PropertyNameCaseInsensitive;
            });

            // Configure EasyReasy.Auth
            string jwtSecret = EnvironmentVariables.JwtSigningSecret.GetValue();
            builder.Services.AddEasyReasyAuth(jwtSecret, issuer: "easyreasy-knowledgebase");

            // Register password hasher
            builder.Services.AddSingleton<IPasswordHasher, SecurePasswordHasher>();

            // Register auth validation service
            builder.Services.AddSingleton<IAuthRequestValidationService, AuthService>();

            // Add services to the container.
            await AiServiceConfigurator.ConfigureAllAiServicesAsync(builder.Services);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            WebApplication app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // Use EasyReasy.Auth middleware (enables progressive delay by default)
            app.UseEasyReasyAuth();

            app.UseAuthorization();

            // Add auth endpoints
            app.AddAuthEndpoints(
                app.Services.GetRequiredService<IAuthRequestValidationService>(),
                allowApiKeys: false,
                allowUsernamePassword: true);

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
