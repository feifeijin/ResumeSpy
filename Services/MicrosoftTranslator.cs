using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ResumeSpy.Services
{
    public class MicrosoftTranslator : BaseTranslator
    {
        public MicrosoftTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        protected override void PrepareHttpClient()
        {
            // Clear headers to avoid duplicate headers if the client is reused
            if (_httpClient.DefaultRequestHeaders.Contains("Ocp-Apim-Subscription-Key"))
            {
                _httpClient.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
            }
            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
        }

        protected override string BuildRequestUrl(string targetLanguage)
        {
            return $"{_endpoint}/translate?api-version=3.0&to={targetLanguage}";
        }

        protected override HttpContent CreateRequestContent(string text, string targetLanguage)
        {
            var requestBody = new object[] { new { Text = text } };
            var content = new StringContent(JsonSerializer.Serialize(requestBody));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }

        protected override string ExtractTranslatedText(string responseBody, string originalText)
        {
            try
            {
                var result = JsonSerializer.Deserialize<List<TranslationResult>>(responseBody);
                return result?.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text ?? originalText;
            }
            catch (JsonException)
            {
                return originalText;
            }
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