using System.Net.Http;
using System.Text.Json;
using System.Collections.Generic;

namespace ResumeSpy.Services
{
    public class LibreTranslator : BaseTranslator
    {
        public LibreTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        protected override string BuildRequestUrl(string targetLanguage)
        {
            // LibreTranslate uses the endpoint directly
            return _endpoint;
        }

        protected override HttpContent CreateRequestContent(string text, string targetLanguage)
        {
            var formContent = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("q", text),
                new KeyValuePair<string, string>("target", targetLanguage)
            };
            
            // Add API key if provided
            if (!string.IsNullOrEmpty(_apiKey))
            {
                formContent.Add(new KeyValuePair<string, string>("api_key", _apiKey));
            }
            
            return new FormUrlEncodedContent(formContent);
        }

        protected override string ExtractTranslatedText(string responseBody, string originalText)
        {
            try
            {
                var result = JsonSerializer.Deserialize<LibreTranslateResponse>(responseBody);
                return result?.TranslatedText ?? originalText;
            }
            catch (JsonException)
            {
                return originalText;
            }
        }

        private class LibreTranslateResponse
        {
            public string TranslatedText { get; set; }
        }
    }
}