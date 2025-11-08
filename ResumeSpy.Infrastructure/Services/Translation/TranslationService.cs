using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    /// <summary>
    /// Implementation of ITranslationService that bridges business layer to infrastructure
    /// Provides clean abstraction over different translation providers
    /// </summary>
    public class TranslationService : ITranslationService
    {
        private readonly TranslatorFactory _translatorFactory;

        public TranslationService(
            IHttpClientFactory httpClientFactory, 
            IOptions<TranslatorSettings> translatorSettings,
            AIOrchestratorService? aiOrchestrator = null)
        {
            _translatorFactory = new TranslatorFactory(httpClientFactory, translatorSettings, aiOrchestrator);
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            var translator = _translatorFactory.CreateTranslator();
            return await translator.DetectLanguageAsync(text);
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            // If source and target are the same, no translation needed
            if (sourceLanguage.Equals(targetLanguage, StringComparison.OrdinalIgnoreCase))
                return text;

            var translator = _translatorFactory.CreateTranslator();
            return await translator.TranslateAsync(text, sourceLanguage, targetLanguage);
        }

        public async Task<string> TranslateTextAsync(string text, string targetLanguage)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetLanguage))
                return text;

            // Auto-detect source language first
            var sourceLanguage = await DetectLanguageAsync(text);

            if (string.IsNullOrWhiteSpace(sourceLanguage))
                sourceLanguage = "en"; // Default to English if detection fails

            return await TranslateTextAsync(text, sourceLanguage, targetLanguage);
        }
    }
}
