using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Generation;
using EasyReasy.KnowledgeBase.OllamaGeneration;

namespace EasyReasy.KnowledgeBase.Web.Server
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            EnvironmentVariableHelper.ValidateVariableNamesIn(typeof(EnvironmentVariables));

            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            await ConfigureServicesAsync(builder.Services);

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

        private static async Task ConfigureServicesAsync(IServiceCollection services)
        {
            // Configure embedding service
            string baseUrl = EnvironmentVariables.OllamaServerUrls.GetAllValues().First();
            string apiKey = EnvironmentVariables.OllamaServerApiKeys.GetAllValues().First();
            string modelName = EnvironmentVariables.OllamaEmbeddingModelName.GetValue();

            IEmbeddingService embeddingService = await EasyReasyOllamaEmbeddingService.CreateAsync(baseUrl, apiKey, modelName);
            services.AddSingleton(embeddingService);

            // Configure one-shot service (for AI text processing like summarization, question generation, etc.)
            IOneShotService oneShotService = await EasyReasyOllamaOneShotService.CreateAsync(baseUrl, apiKey, modelName);
            services.AddSingleton(oneShotService);
        }
    }
}
