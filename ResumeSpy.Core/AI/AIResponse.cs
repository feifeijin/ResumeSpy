namespace ResumeSpy.Core.AI
{
    /// <summary>
    /// Standardized response model from AI operations across all providers
    /// </summary>
    public class AIResponse
    {
        /// <summary>
        /// The generated content from the AI model
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the request was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error message if the request failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Calculated cost for this operation in USD
        /// </summary>
        public decimal Cost { get; set; }

        /// <summary>
        /// Time taken to complete the request
        /// </summary>
        public TimeSpan Latency { get; set; }

        /// <summary>
        /// Number of tokens in the prompt
        /// </summary>
        public int PromptTokens { get; set; }

        /// <summary>
        /// Number of tokens in the completion
        /// </summary>
        public int CompletionTokens { get; set; }

        /// <summary>
        /// Total tokens used (prompt + completion)
        /// </summary>
        public int TotalTokens => PromptTokens + CompletionTokens;

        /// <summary>
        /// Name of the provider that handled this request
        /// </summary>
        public string? ProviderName { get; set; }

        /// <summary>
        /// Actual model used (may differ from requested if fallback occurred)
        /// </summary>
        public string? ModelUsed { get; set; }
    }
}
