using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using ResumeSpy.Interfaces;

namespace ResumeSpy.Services
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

        // Template method - defines the skeleton of the algorithm
        public async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            PrepareHttpClient();
            var requestUrl = BuildRequestUrl(targetLanguage);
            var requestContent = CreateRequestContent(text, targetLanguage);
            
            var response = await _httpClient.PostAsync(requestUrl, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            
            return ExtractTranslatedText(responseBody, text);
        }

        // Hook methods that subclasses can override
        protected virtual void PrepareHttpClient() { }
        
        // Abstract methods that subclasses must implement
        protected abstract string BuildRequestUrl(string targetLanguage);
        protected abstract HttpContent CreateRequestContent(string text, string targetLanguage);
        protected abstract string ExtractTranslatedText(string responseBody, string originalText);
    }
}