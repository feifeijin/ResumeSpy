using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.AI;

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
            
            var systemMessage = "You are a professional translator. Provide only the translated text without any explanations or additional commentary.";
            
            var prompt = $"Translate the following text to {languageNames}";
            
            if (!string.IsNullOrWhiteSpace(context))
            {
                prompt += $"\n\nContext: {context}";
            }
            
            if (!string.IsNullOrWhiteSpace(sourceLanguage))
            {
                var sourceName = GetLanguageName(sourceLanguage);
                prompt += $"\n\nSource language: {sourceName}";
            }
            
            prompt += $"\n\nText to translate:\n{text}";

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
            var systemMessage = "You are a language detection expert. Respond with ONLY the ISO 639-1 language code (e.g., 'en', 'zh', 'ja'), nothing else.";
            
            var prompt = $"Detect the language of this text:\n\n{text}";

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