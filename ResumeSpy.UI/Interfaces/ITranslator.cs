using System.Threading.Tasks;

namespace ResumeSpy.UI.Interfaces
{
    public interface ITranslator
    {
        
        Task<string> DetectLanguageAsync(string text);

        /// <summary>
        /// Translate text
        /// </summary>
        /// <param name="text">Original text</param>
        /// <param name="sourceLanguage">Source language (e.g., "en")</param>
        /// <param name="targetLanguage">Target language (e.g., "zh", "en")</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);
    }
}