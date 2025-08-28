using System.Text.Json;
using EasyReasy.KnowledgeBase.Web.Server.Configuration;

namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    public class StreamChatResponse
    {
        public string? Message { get; set; }
        
        public string? Error { get; set; }

        public static StreamChatResponse CreateMessage(string message)
        {
            return new StreamChatResponse { Message = message };
        }

        public static StreamChatResponse CreateError(string error)
        {
            return new StreamChatResponse { Error = error };
        }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonConfiguration.DefaultOptions);
        }
    }
}
