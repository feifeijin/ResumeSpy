using System.Net.Http;
using ResumeSpy.Interfaces;

namespace ResumeSpy.Services
{
    public abstract class BaseTranslator : ITranslator
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _apiKey;

        protected BaseTranslator(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public abstract Task<string> TranslateAsync(string text, string targetLanguage);
    }
}