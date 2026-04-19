namespace ResumeSpy.Infrastructure.Prompts
{
    /// <summary>
    /// Prompt templates for AI-powered translation and language detection.
    /// </summary>
    internal static class TranslationPrompts
    {
        internal const string TranslationSystemMessage =
            "You are a professional translator. Provide only the translated text without any explanations or additional commentary.";

        /// <summary>
        /// Builds the translation user prompt for the given target language and optional context.
        /// </summary>
        internal static string BuildTranslationPrompt(string text, string targetLanguageName, string? sourceName, string? context)
        {
            var prompt = $"Translate the following text to {targetLanguageName}";

            if (!string.IsNullOrWhiteSpace(context))
                prompt += $"\n\nContext: {context}";

            if (!string.IsNullOrWhiteSpace(sourceName))
                prompt += $"\n\nSource language: {sourceName}";

            prompt += $"\n\nText to translate:\n{text}";

            return prompt;
        }

        internal const string LanguageDetectionSystemMessage =
            @"You are a language detection expert. Analyze the PRIMARY language used to write the text based on its SCRIPT and GRAMMAR structure ONLY.

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

        internal static string BuildLanguageDetectionPrompt(string text) =>
            $"What language is this text written in?\n\n{text}";
    }
}
