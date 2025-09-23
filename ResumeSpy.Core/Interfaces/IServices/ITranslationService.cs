using System.Threading.Tasks;

namespace ResumeSpy.Core.Interfaces.IServices
{
    /// <summary>
    /// Translation service abstraction for business layer
    /// Provides high-level translation operations without exposing infrastructure details
    /// </summary>
    public interface ITranslationService
    {
        /// <summary>
        /// Detects the language of the provided text
        /// </summary>
        /// <param name="text">Text to analyze</param>
        /// <returns>Detected language code (e.g., "en", "zh", "es")</returns>
        Task<string> DetectLanguageAsync(string text);

        /// <summary>
        /// Translates text from source language to target language
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="sourceLanguage">Source language code (e.g., "en")</param>
        /// <param name="targetLanguage">Target language code (e.g., "zh", "es")</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage);

        /// <summary>
        /// Auto-detects source language and translates to target language
        /// </summary>
        /// <param name="text">Text to translate</param>
        /// <param name="targetLanguage">Target language code</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateTextAsync(string text, string targetLanguage);
    }
}
