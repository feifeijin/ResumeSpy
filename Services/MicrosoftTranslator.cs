using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResumeSpy.Services
{
    public class MicrosoftTranslator : BaseTranslator
    {
        private readonly string _endpoint;

        public MicrosoftTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey)
        {
            _endpoint = endpoint;
        }

        public override async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            var route = $"/translate?api-version=3.0&to={targetLanguage}";
            var requestBody = new object[] { new { Text = text } };
            var requestContent = new StringContent(JsonSerializer.Serialize(requestBody));
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
            var response = await _httpClient.PostAsync(_endpoint + route, requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<List<TranslationResult>>(responseBody);
            return result?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text ?? text;
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