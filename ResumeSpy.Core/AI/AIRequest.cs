namespace ResumeSpy.Core.AI
{
    /// <summary>
    /// Standardized request model for AI operations across all providers
    /// </summary>
    public class AIRequest
    {
        /// <summary>
        /// The main prompt/instruction for the AI model
        /// </summary>
        public required string Prompt { get; set; }

        /// <summary>
        /// Optional system-level instruction to set the behavior of the AI
        /// </summary>
        public string? SystemMessage { get; set; }

        /// <summary>
        /// Controls randomness (0.0 = deterministic, 2.0 = very creative)
        /// </summary>
        public double Temperature { get; set; } = 0.7;

        /// <summary>
        /// Maximum number of tokens to generate
        /// </summary>
        public int MaxTokens { get; set; } = 1024;

        /// <summary>
        /// Specific model to use (e.g., "gpt-4-turbo", "gpt-3.5-turbo")
        /// If null, provider will use its default
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// Optional metadata for tracking/logging purposes
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }
}
