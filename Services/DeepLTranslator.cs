using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ResumeSpy.Services
{
    public class DeepLTranslator : BaseTranslator
    {
        public DeepLTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        protected override string BuildRequestUrl(string targetLanguage)
        {
            // DeepL uses the endpoint directly
            return _endpoint;
        }

        protected override HttpContent CreateRequestContent(string text, string targetLanguage)
        {
            return new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("auth_key", _apiKey),
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("target_lang", targetLanguage)
            });
        }

        protected override string ExtractTranslatedText(string responseBody, string originalText)
        {
            try
            {
                var result = JsonSerializer.Deserialize<DeepLResponse>(responseBody);
                return result?.Translations?.FirstOrDefault()?.Text ?? originalText;
            }
            catch (JsonException)
            {
                return originalText;
            }
        }

        private class DeepLResponse
        {
            public List<DeepLTranslation> Translations { get; set; }
        }

        private class DeepLTranslation
        {
            public string Text { get; set; }
        }
    }
}