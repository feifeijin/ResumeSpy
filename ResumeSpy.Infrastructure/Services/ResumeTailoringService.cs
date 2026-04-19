using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Prompts;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services
{
    public class ResumeTailoringService : IResumeTailoringService
    {
        private readonly AIOrchestratorService _aiOrchestrator;
        private readonly ILogger<ResumeTailoringService> _logger;

        public ResumeTailoringService(
            AIOrchestratorService aiOrchestrator,
            ILogger<ResumeTailoringService> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
        }

        public async Task<string> TailorResumeAsync(
            string resumeContent,
            string jobDescription,
            string? language = null)
        {
            var request = new AIRequest
            {
                Prompt = TailoringPrompts.BuildPrompt(resumeContent, jobDescription),
                SystemMessage = TailoringPrompts.SystemMessage,
                Temperature = 0.4,
                MaxTokens = 4096
            };

            _logger.LogInformation("Starting AI resume tailoring for language: {Language}", language ?? "unspecified");

            var response = await _aiOrchestrator.ExecuteTextGenerationAsync(request, useCache: false);

            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogError("AI tailoring failed: {Error}", response.ErrorMessage);
                throw new InvalidOperationException($"AI tailoring failed: {response.ErrorMessage}");
            }

            _logger.LogInformation("AI tailoring succeeded via {Provider}", response.ProviderName);
            return response.Content.Trim();
        }
    }
}
