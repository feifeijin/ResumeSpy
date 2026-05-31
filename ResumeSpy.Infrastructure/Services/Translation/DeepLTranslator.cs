using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace ResumeSpy.Infrastructure.Services.Translation
{
    /// <summary>
    /// DeepL translator. Sends raw content to the DeepL API as-is. Unlike the AI
    /// translator, this path cannot enforce résumé-specific rules (keep personal /
    /// company names, translate job titles but not certifications, preserve Markdown);
    /// DeepL receives no such instructions. Use TranslatorType.AI for résumé-aware
    /// translation. See TranslationPrompts for the AI rule set.
    /// </summary>
    internal class DeepLTranslator : BaseTranslator
    {
        public DeepLTranslator(HttpClient httpClient, string apiKey, string endpoint)
            : base(httpClient, apiKey, endpoint)
        {
        }

        public override async Task<string> TranslateAsync(string text, string sourceLanguage, string targetLanguage)
        {
            // Use the injected HttpClient (named "Translation") so DeepL calls flow
            // through the resilience handler — retries, circuit breaker, timeouts —
            // configured in ServiceExtension. A `new HttpClient()` here would bypass
            // all of it and also leak sockets.
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
            var response = await _httpClient.SendAsync(request);
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
            public string DetectedSourceLanguage { get; set; } = string.Empty;

            public string Text { get; set; } = string.Empty;
        }

        public class DeepLResponse
        {
            public List<Translation> Translations { get; set; } = new();
        }
    }
}
