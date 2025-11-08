using Microsoft.Extensions.Options;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Services.AI;
using ResumeSpy.Core.Interfaces.AI;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    /// <summary>
    /// AI-powered translator implementation using the AI orchestrator
    /// </summary>
    internal class AITranslator : ITranslator
    {
        private readonly AIOrchestratorService _aiOrchestrator;
        private readonly AITranslatorSettings? _settings;

        public AITranslator(AIOrchestratorService aiOrchestrator, AITranslatorSettings? settings = null)
        {
            _aiOrchestrator = aiOrchestrator;
            _settings = settings;
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var translationService = _aiOrchestrator.GetTranslationService();
            return await translationService.DetectLanguageAsync(text);
        }

        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            // If source and target are the same, no translation needed
            if (sourceLanguage.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase))
                return text;

            var translationService = _aiOrchestrator.GetTranslationService();
            
            // Use configured context if available
            var context = _settings?.DefaultContext;
            
            return await translationService.TranslateAsync(text, targetLanguage, sourceLanguage, context);
        }
    }
}