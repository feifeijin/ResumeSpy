using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
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
            var systemMessage = """
                You are a professional resume writer and career coach.
                Your task is to tailor a resume to better match a specific job description.

                Rules:
                - Keep the exact same markdown structure and formatting as the original
                - Preserve ALL factual information: company names, dates, education, contact info, job titles
                - Reorder and emphasize skills and experiences that are most relevant to the job
                - Naturally incorporate keywords from the job description where appropriate
                - Strengthen bullet points to highlight achievements relevant to the role
                - Do NOT fabricate, exaggerate, or invent any experience or qualifications
                - Return ONLY the tailored resume in markdown format — no explanations, no preamble
                """;

            var prompt = $"""
                ## Original Resume:

                {resumeContent}

                ## Job Description:

                {jobDescription}

                Tailor the resume to better match this job description. Follow all rules strictly.
                """;

            var request = new AIRequest
            {
                Prompt = prompt,
                SystemMessage = systemMessage,
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
