using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace ResumeSpy.Services
{
    public class LibreTranslator : BaseTranslator
    {
        public LibreTranslator(HttpClient httpClient, string apiKey)
            : base(httpClient, apiKey)
        {
        }

        public override async Task<string> TranslateAsync(string text, string targetLanguage)
        {
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("q", text),
                new KeyValuePair<string, string>("target", targetLanguage)
            });

            var response = await _httpClient.PostAsync("https://libretranslate.com/translate", requestContent);
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<LibreTranslateResult>(responseBody);
            return result?.TranslatedText ?? text;
        }

        private class LibreTranslateResult
        {
            public string TranslatedText { get; set; }
        }
    }
}