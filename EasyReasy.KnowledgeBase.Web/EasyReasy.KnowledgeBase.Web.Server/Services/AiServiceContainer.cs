namespace EasyReasy.KnowledgeBase.Web.Server.Services
{
    /// <summary>
    /// Generic container for AI services with different model sizes.
    /// Can be used for one-shot services, text completion services, or any other AI service type.
    /// </summary>
    /// <typeparam name="T">The type of AI service to contain (e.g., IOneShotService, ITextCompletionService)</typeparam>
    public class AiServiceContainer<T> where T : class
    {
        /// <summary>
        /// Small model service for quick, lightweight tasks.
        /// </summary>
        public T Small { get; }

        /// <summary>
        /// Large model service for balanced performance and quality.
        /// </summary>
        public T Large { get; }

        /// <summary>
        /// Reasoning model service for complex reasoning and analysis tasks.
        /// </summary>
        public T Reasoning { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AiServiceContainer{T}"/> class.
        /// </summary>
        /// <param name="small">The small model service instance.</param>
        /// <param name="large">The large model service instance.</param>
        /// <param name="reasoning">The reasoning model service instance.</param>
        public AiServiceContainer(T small, T large, T reasoning)
        {
            Small = small ?? throw new ArgumentNullException(nameof(small));
            Large = large ?? throw new ArgumentNullException(nameof(large));
            Reasoning = reasoning ?? throw new ArgumentNullException(nameof(reasoning));
        }
    }
}
