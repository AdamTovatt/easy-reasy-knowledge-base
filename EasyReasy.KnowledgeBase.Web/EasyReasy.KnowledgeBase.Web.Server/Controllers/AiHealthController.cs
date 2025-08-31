using EasyReasy.KnowledgeBase.Web.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EasyReasy.KnowledgeBase.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AiHealthController : ControllerBase
    {
        private readonly EmbeddingServiceProvider _embeddingServiceProvider;
        private readonly OneShotServiceContainerProvider _oneShotServiceContainerProvider;
        private readonly OllamaClientProvider _ollamaClientProvider;

        public AiHealthController(
            EmbeddingServiceProvider embeddingServiceProvider,
            OneShotServiceContainerProvider oneShotServiceContainerProvider,
            OllamaClientProvider ollamaClientProvider)
        {
            _embeddingServiceProvider = embeddingServiceProvider;
            _oneShotServiceContainerProvider = oneShotServiceContainerProvider;
            _ollamaClientProvider = ollamaClientProvider;
        }

        [HttpGet]
        public IActionResult GetHealthStatus()
        {
            var healthStatus = new
            {
                embeddingService = new
                {
                    isAvailable = _embeddingServiceProvider.IsAvailable,
                    errorMessage = _embeddingServiceProvider.ErrorMessage
                },
                oneShotServiceContainer = new
                {
                    isAvailable = _oneShotServiceContainerProvider.IsAvailable,
                    errorMessage = _oneShotServiceContainerProvider.ErrorMessage
                },
                ollamaClient = new
                {
                    isAvailable = _ollamaClientProvider.IsAvailable,
                    errorMessage = _ollamaClientProvider.ErrorMessage
                }
            };

            return Ok(healthStatus);
        }
    }
}
