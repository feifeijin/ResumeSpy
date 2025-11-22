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
            var systemMessage = @"You are a language detection expert. Analyze the PRIMARY language used to write the text based on its SCRIPT and GRAMMAR structure ONLY. 

CRITICAL RULES:
1. IGNORE all language names mentioned in the content (e.g., 'Japanese', 'English', 'Spanish', etc.)
2. IGNORE country names (e.g., 'Japan', 'China', 'Korea', etc.)
3. IGNORE language proficiency indicators (e.g., 'N1', 'N2', 'JLPT', 'TOEIC', etc.)
4. Look ONLY at the actual characters and sentence structure being used

EXAMPLES:
- Text: 'I speak Japanese (N1)' → Answer: 'en' (written in English, just mentions Japanese)
- Text: 'Languages: English, Japanese, Chinese' → Answer: 'en' (the word 'Languages' is English)
- Text: 'Japan is a beautiful country' → Answer: 'en' (written in English, just mentions Japan)
- Text: '私は日本人です' → Answer: 'ja' (written with Hiragana/Kanji)

SCRIPT DETECTION:
- Japanese: MUST contain Hiragana (ひらがな) or Katakana (カタカナ) → 'ja'
- Korean: MUST contain Hangul (한글) → 'ko'
- Chinese: Chinese characters with Chinese grammar patterns → 'zh'
- English: Latin alphabet with English grammar (articles, verb conjugations) → 'en'
- Spanish: Latin alphabet with Spanish grammar (gender agreement, verb forms) → 'es'
- French: Latin alphabet with French grammar (articles le/la, accents) → 'fr'
- German: Latin alphabet with German grammar (capitalized nouns, cases) → 'de'
- Portuguese: Latin alphabet with Portuguese grammar → 'pt'
- Russian: MUST contain Cyrillic script (Кириллица) → 'ru'
- Arabic: MUST contain Arabic script (العربية) → 'ar'
- Italian: Latin alphabet with Italian grammar → 'it'
- Thai: MUST contain Thai script (ไทย) → 'th'
- Vietnamese: Latin alphabet with Vietnamese diacritics (ă, ơ, ê, ô, ư) → 'vi'
- Hindi: MUST contain Devanagari script (देवनागरी) → 'hi'
- Turkish: Latin alphabet with Turkish grammar and letters (ı, ş, ğ) → 'tr'

Respond with ONLY the ISO 639-1 language code (2 letters). Nothing else.";
            
            var prompt = $"What language is this text written in?\n\n{text}";

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