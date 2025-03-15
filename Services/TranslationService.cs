using System.Threading.Tasks;
using ResumeSpy.Interfaces;

namespace ResumeSpy.Services
{
    public class TranslationService
    {
        private readonly ITranslator _translator;

        public TranslationService(ITranslator translator)
        {
            _translator = translator;
        }

        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            return await _translator.TranslateAsync(text, targetLanguage);
        }
    }
}