using System.Text.Json;

namespace EasyReasy.KnowledgeBase.Web.Server.Configuration
{
    public static class JsonConfiguration
    {
        public static JsonSerializerOptions DefaultOptions { get; } = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }
}
