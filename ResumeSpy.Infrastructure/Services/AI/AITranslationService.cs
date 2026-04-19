using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;
using ResumeSpy.Infrastructure.Prompts;

namespace ResumeSpy.Infrastructure.Services.AI
{
    /// <summary>
    /// AI-powered translation service using generative text models
    /// </summary>
    public class AITranslationService : IAITranslationService
    {
        private readonly IGenerativeTextService _textService;
        private readonly ILogger<AITranslationService> _logger;

        public AITranslationService(IGenerativeTextService textService, ILogger<AITranslationService> logger)
        {
            _textService = textService;
            _logger = logger;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null, string? context = null)
        {
            var languageNames = GetLanguageName(targetLanguage);
            
            var systemMessage = TranslationPrompts.TranslationSystemMessage;
            var sourceName = string.IsNullOrWhiteSpace(sourceLanguage) ? null : GetLanguageName(sourceLanguage);
            var prompt = TranslationPrompts.BuildTranslationPrompt(text, languageNames, sourceName, context);

            var request = new AIRequest
            {
                Prompt = prompt,
                SystemMessage = systemMessage,
                Temperature = 0.3, // Lower temperature for more consistent translations
                MaxTokens = 2048,
                Metadata = new Dictionary<string, string>
                {
                    { "operation", "translate" },
                    { "targetLanguage", targetLanguage }
                }
            };

            var response = await _textService.GenerateResponseAsync(request);

            if (!response.IsSuccess)
            {
                _logger.LogError("Translation failed: {Error}", response.ErrorMessage);
                throw new Exception($"Translation failed: {response.ErrorMessage}");
            }

            return response.Content.Trim();
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            var systemMessage = TranslationPrompts.LanguageDetectionSystemMessage;
            var prompt = TranslationPrompts.BuildLanguageDetectionPrompt(text);

            var request = new AIRequest
            {
                Prompt = prompt,
                SystemMessage = systemMessage,
                Temperature = 0.1,
                MaxTokens = 10,
                Metadata = new Dictionary<string, string>
                {
                    { "operation", "detect_language" }
                }
            };

            var response = await _textService.GenerateResponseAsync(request);

            if (!response.IsSuccess)
            {
                _logger.LogError("Language detection failed: {Error}", response.ErrorMessage);
                throw new Exception($"Language detection failed: {response.ErrorMessage}");
            }

            return response.Content.Trim().ToLowerInvariant();
        }

        private static string GetLanguageName(string languageCode)
        {
            return languageCode.ToLowerInvariant() switch
            {
                "en" or "en-us" => "English",
                "zh" or "zh-cn" or "zh-hans" => "Simplified Chinese",
                "zh-tw" or "zh-hant" => "Traditional Chinese",
                "ja" or "ja-jp" => "Japanese",
                "ko" or "ko-kr" => "Korean",
                "es" => "Spanish",
                "fr" => "French",
                "de" => "German",
                "it" => "Italian",
                "pt" => "Portuguese",
                "ru" => "Russian",
                "ar" => "Arabic",
                _ => languageCode
            };
        }
    }
}