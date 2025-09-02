using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using EasyReasy.KnowledgeBase.Web.Server.Repositories;
using EasyReasy.KnowledgeBase.Web.Server.Database;
using EasyReasy.KnowledgeBase.Storage;
using EasyReasy.Auth;
using Microsoft.AspNetCore.Http.Json;
using EasyReasy.KnowledgeBase.Web.Server.Services.Auth;
using EasyReasy.KnowledgeBase.Web.Server.Services.Account;
using EasyReasy.KnowledgeBase.Web.Server.Services.Storage;
using EasyReasy.FileStorage;
using Microsoft.Extensions.Caching.Memory;

namespace EasyReasy.KnowledgeBase.Web.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await Task.CompletedTask;

            EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(EnvironmentVariable));

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Configure JSON serialization to use camelCase
            builder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.PropertyNamingPolicy = JsonConfiguration.DefaultOptions.PropertyNamingPolicy;
                options.SerializerOptions.PropertyNameCaseInsensitive = JsonConfiguration.DefaultOptions.PropertyNameCaseInsensitive;
            });

            // Add memory cache for upload sessions
            builder.Services.AddMemoryCache();

            // Configure EasyReasy.Auth
            string jwtSecret = EnvironmentVariable.JwtSigningSecret.GetValue();
            builder.Services.AddEasyReasyAuth(jwtSecret, issuer: "easyreasy-knowledgebase");

            // Register password hasher
            builder.Services.AddSingleton<IPasswordHasher, SecurePasswordHasher>();

            // Register auth validation service
            builder.Services.AddSingleton<IAuthRequestValidationService, AuthService>();

            // Register database connection factory
            string postgresConnectionString = EnvironmentVariable.PostgresConnectionString.GetValue();
            builder.Services.AddSingleton<IDbConnectionFactory>(new PostgresConnectionFactory(postgresConnectionString));

            // Register repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IKnowledgeFileRepository, KnowledgeFileRepository>();

            // Register file storage system
            string fileStorageBasePath = EnvironmentVariable.FileStorageBasePath.GetValue();
            builder.Services.AddSingleton<IFileSystem>(new LocalFileSystem(fileStorageBasePath));

            // Get required max file size from environment
            string maxFileSizeString = EnvironmentVariable.MaxFileSizeBytes.GetValue();
            if (!long.TryParse(maxFileSizeString, out long maxFileSizeBytes) || maxFileSizeBytes <= 0)
            {
                throw new InvalidOperationException($"MAX_FILE_SIZE_BYTES must be a positive integer, got: '{maxFileSizeString}'");
            }

            // Register services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IFileStorageService>(serviceProvider => 
                new FileStorageService(
                    serviceProvider.GetRequiredService<IFileSystem>(),
                    serviceProvider.GetRequiredService<IKnowledgeFileRepository>(),
                    serviceProvider.GetRequiredService<IMemoryCache>(),
                    maxFileSizeBytes,
                    serviceProvider.GetRequiredService<ILogger<FileStorageService>>()
                ));

            // Add services to the container.
            AiServiceConfigurator.ConfigureAllAiServices(builder.Services);

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            WebApplication app = builder.Build();

            // Preload AI services in background (this will capture error messages for health reporting)
            _ = Task.Run(async () => await AiServiceConfigurator.PreloadAllAiServicesAsync(app.Services));

            // Run database migrations
            ILogger logger = app.Services.GetRequiredService<ILogger<Program>>();
            bool migrationSuccess = DatabaseMigrator.RunMigrations(postgresConnectionString, logger);

            if (!migrationSuccess)
            {
                logger.LogError("Database migration failed. Application will exit.");
                return;
            }

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
