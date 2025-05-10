using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeSpy.UI.Services
{
    public class MicrosoftTranslator : BaseTranslator
    {
        public MicrosoftTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        public override Task<string> DetectLanguageAsync(string text)
        {
            throw new NotImplementedException();
        }

        public override Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            throw new NotImplementedException();
        }

        private class TranslationResult
        {
            public List<Translation> Translations { get; set; }
        }

        private class Translation
        {
            public string Text { get; set; }
        }
    }
}