using EasyReasy.KnowledgeBase.Web.Server.Configuration;
using System.Text.Json;

namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a response from the streaming chat service, containing either a message or an error.
    /// </summary>
    public class StreamChatResponse
    {
        /// <summary>
        /// Gets or sets the message content from the AI service, or null if there was an error.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets the error message if the request failed, or null if successful.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// Creates a new <see cref="StreamChatResponse"/> with a message.
        /// </summary>
        /// <param name="message">The message content from the AI service.</param>
        /// <returns>A new <see cref="StreamChatResponse"/> containing the message.</returns>
        public static StreamChatResponse CreateMessage(string message)
        {
            return new StreamChatResponse { Message = message };
        }

        /// <summary>
        /// Creates a new <see cref="StreamChatResponse"/> with an error.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <returns>A new <see cref="StreamChatResponse"/> containing the error.</returns>
        public static StreamChatResponse CreateError(string error)
        {
            return new StreamChatResponse { Error = error };
        }

        /// <summary>
        /// Serializes this response to JSON using the default configuration.
        /// </summary>
        /// <returns>A JSON string representation of this response.</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonConfiguration.DefaultOptions);
        }
    }
}
