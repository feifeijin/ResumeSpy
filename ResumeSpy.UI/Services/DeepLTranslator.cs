using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ResumeSpy.UI.Services
{
    public class DeepLTranslator : BaseTranslator
    {
        public DeepLTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Authorization", $"DeepL-Auth-Key {_apiKey}");
            var collection = new List<KeyValuePair<string, string>>
            {
                new("text", text),
                new("target_lang", targetLanguage),
                new("source_lang", sourceLanguage??"en")
            };
            var content = new FormUrlEncodedContent(collection);
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseBody = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonConvert.DeserializeObject<DeepLResponse>(responseBody);
                return result?.Translations?.FirstOrDefault()?.Text ?? text;
            }
            catch (JsonException)
            {
                return text;
            }

        }

        public override async Task<string> DetectLanguageAsync(string text)
        {
            return await Task.FromResult(string.Empty);
        }

        public class Translation
        {
            [JsonProperty("detected_source_language")]
            public string DetectedSourceLanguage { get; set; }

            public string Text { get; set; }
        }

        public class DeepLResponse
        {
            public List<Translation> Translations { get; set; }
        }
    }
}