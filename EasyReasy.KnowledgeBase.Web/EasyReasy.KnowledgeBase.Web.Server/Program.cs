using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;
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

            app.UseAuthorization();

            app.MapControllers();

            app.MapFallbackToFile("/index.html");

            app.Run();
        }
    }
}
