using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using ResumeSpy.UI.Interfaces;

namespace ResumeSpy.UI.Services
{
    public abstract class BaseTranslator : ITranslator
    {
        protected readonly HttpClient _httpClient;
        protected readonly string _apiKey;
        protected readonly string _endpoint;

        protected BaseTranslator(HttpClient httpClient, string apiKey, string endpoint)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _endpoint = endpoint;
        }

        public abstract Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage);


        public abstract Task<string> DetectLanguageAsync(string text);
    }
}