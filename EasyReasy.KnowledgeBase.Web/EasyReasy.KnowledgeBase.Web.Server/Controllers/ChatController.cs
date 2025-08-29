using EasyReasy.EnvironmentVariables;
using EasyReasy.KnowledgeBase.Web.Server.Models;
using EasyReasy.Ollama.Client;
using EasyReasy.Ollama.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EasyReasy.Auth;

namespace EasyReasy.KnowledgeBase.Web.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly OllamaClient _ollamaClient;

        public ChatController(OllamaClient ollamaClient)
        {
            _ollamaClient = ollamaClient;
        }

        [HttpPost("stream")]
        public async Task<IActionResult> StreamChat([FromBody] StreamChatRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            // Access user information from the JWT token
            string? userId = HttpContext.GetUserId();
            string? tenantId = HttpContext.GetTenantId();
            IEnumerable<string> roles = HttpContext.GetRoles();

            // Log user information (you can remove this in production)
            Console.WriteLine($"User {userId} from tenant {tenantId} with roles: {string.Join(", ", roles)}");

            string modelName = EnvironmentVariables.OllamaSmallTextCompletionModelName.GetValue();

            Response.Headers.ContentType = "text/event-stream";
            Response.Headers.CacheControl = "no-cache";
            Response.Headers.Connection = "keep-alive";

            try
            {
                await foreach (ChatResponsePart part in _ollamaClient.Chat.StreamChatAsync(modelName, request.Message, cancellationToken))
                {
                    if (part.Message != null)
                    {
                        StreamChatResponse response = StreamChatResponse.CreateMessage(part.Message);
                        string sseData = $"data: {response.ToJson()}\n\n";
                        await Response.WriteAsync(sseData);
                        await Response.Body.FlushAsync();
                    }
                }
            }
            catch (Exception exception)
            {
                StreamChatResponse errorResponse = StreamChatResponse.CreateError(exception.Message);
                string errorData = $"data: {errorResponse.ToJson()}\n\n";
                await Response.WriteAsync(errorData);
                await Response.Body.FlushAsync();
            }

            return new EmptyResult();
        }
    }
}
