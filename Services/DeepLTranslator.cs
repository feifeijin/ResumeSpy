using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResumeSpy.Services
{
    public class DeepLTranslator : BaseTranslator
    {
        public DeepLTranslator(HttpClient httpClient, string apiKey)
            : base(httpClient, apiKey)
        {
        }

        public override async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("auth_key", _apiKey),
                new KeyValuePair<string, string>("text", text),
                new KeyValuePair<string, string>("target_lang", targetLanguage)
            });

            var response = await _httpClient.PostAsync("https://api.deepl.com/v2/translate", requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<DeepLTranslationResult>(responseBody);
            return result?.Translations?.FirstOrDefault()?.Text ?? text;
        }

        private class DeepLTranslationResult
        {
            public List<DeepLTranslation> Translations { get; set; }
        }

        private class DeepLTranslation
        {
            public string Text { get; set; }
        }
    }
}