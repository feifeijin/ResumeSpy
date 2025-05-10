using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace ResumeSpy.UI.Services
{
    public class LibreTranslator : BaseTranslator
    {
        public LibreTranslator(HttpClient httpClient, string apiKey, string endpoint)
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

        private class LibreTranslateResponse
        {
            public string TranslatedText { get; set; }
        }
    }
}