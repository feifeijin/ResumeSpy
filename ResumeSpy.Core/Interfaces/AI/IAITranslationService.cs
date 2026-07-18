namespace ResumeSpy.Core.Interfaces.AI
{
    /// <summary>
    /// Interface for AI-powered translation services
    /// </summary>
    public interface IAITranslationService
    {
        /// <summary>
        /// Translate text to the target language
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language code (e.g., "zh-CN", "ja", "en")</param>
        /// <param name="sourceLanguage">Optional source language code (auto-detect if null)</param>
        /// <param name="context">Optional context to improve translation quality</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateAsync(string text, string targetLanguage, string? sourceLanguage = null, string? context = null);

        /// <summary>
        /// Detect the language of the provided text
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Detected language code</returns>
        Task<string> DetectLanguageAsync(string text);
    }
}
