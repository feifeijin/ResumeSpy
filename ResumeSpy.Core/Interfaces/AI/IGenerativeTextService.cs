using ResumeSpy.Core.AI;

namespace ResumeSpy.Core.Interfaces.AI
{
    /// <summary>
    /// Interface for generative AI text services (LLMs)
    /// </summary>
    public interface IGenerativeTextService
    {
        /// <summary>
        /// Generate a text response based on the provided request
        /// </summary>
        /// <param name="request">The AI request containing prompt and parameters</param>
        /// <returns>The AI response with generated content and metadata</returns>
        Task<AIResponse> GenerateResponseAsync(AIRequest request);
    }
}
