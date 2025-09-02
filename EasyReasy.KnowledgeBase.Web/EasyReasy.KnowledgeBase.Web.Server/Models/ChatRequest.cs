namespace EasyReasy.KnowledgeBase.Web.Server.Models
{
    /// <summary>
    /// Represents a request to stream a chat message to the AI service.
    /// </summary>
    public class StreamChatRequest
    {
        /// <summary>
        /// Gets or sets the message content to send to the AI service.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamChatRequest"/> class with the specified message.
        /// </summary>
        /// <param name="message">The message content to send to the AI service.</param>
        public StreamChatRequest(string message)
        {
            Message = message;
        }
    }
}
