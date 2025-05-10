using System.Threading.Tasks;
using ResumeSpy.UI.Interfaces;

namespace ResumeSpy.UI.Services
{
    public class TranslationService
    {
        private readonly ITranslator _translator;

        public TranslationService(ITranslator translator)
        {
            _translator = translator;
        }

        public async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            return await _translator.TranslateAsync(text,sourceLanguage, targetLanguage);
        }

        public async Task<string> DetectLanguageAsync(string text)
        {
            return await _translator.DetectLanguageAsync(text);
        }
    }
}