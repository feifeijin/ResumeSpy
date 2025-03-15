using System.Threading.Tasks;

namespace ResumeSpy.Interfaces
{
    public interface ITranslator
    {
        /// <summary>
        /// Translate text
        /// </summary>
        /// <param name="text">Original text</param>
        /// <param name="targetLanguage">Target language (e.g., "zh", "en")</param>
        /// <returns>Translated text</returns>
        Task<string> TranslateAsync(string text, string targetLanguage);
    }
}