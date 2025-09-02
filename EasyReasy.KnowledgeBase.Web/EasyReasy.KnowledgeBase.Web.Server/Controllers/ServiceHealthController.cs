using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.KnowledgeBase.Web.Server.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyReasy.KnowledgeBase.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ServiceHealthController : ControllerBase
    {
        private readonly EmbeddingServiceProvider _embeddingServiceProvider;
        private readonly OneShotServiceContainerProvider _oneShotServiceContainerProvider;
        private readonly OllamaClientProvider _ollamaClientProvider;

        public ServiceHealthController(
            EmbeddingServiceProvider embeddingServiceProvider,
            OneShotServiceContainerProvider oneShotServiceContainerProvider,
            OllamaClientProvider ollamaClientProvider)
        {
            _embeddingServiceProvider = embeddingServiceProvider;
            _oneShotServiceContainerProvider = oneShotServiceContainerProvider;
            _ollamaClientProvider = ollamaClientProvider;
        }

        [HttpGet]
        public async Task<IActionResult> GetHealthStatus([FromQuery] bool refresh = false)
        {
            List<IServiceHealthReport> services = new()
            {
                _embeddingServiceProvider,
                _oneShotServiceContainerProvider,
                _ollamaClientProvider,
            };

            // Refresh services if requested
            if (refresh)
            {
                // Start all refresh tasks concurrently
                Task[] refreshTasks = services.Select(service => service.RefreshAsync()).ToArray();

                // Await all refresh tasks to complete
                await Task.WhenAll(refreshTasks);
            }

            ServiceHealthResponse response = new(services);
            return Ok(response);
        }
    }
}
