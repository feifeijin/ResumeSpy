using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeSpy.Core.Interfaces.IServices;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ResumeSpy.Infrastructure.Services
{
    public class ImageGenerationService : IImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageGenerationService> _logger;
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;
        private readonly string _storageBucket;

        // Primary font family name as registered with QuestPDF (matches the font's internal name)
        private const string PrimaryFontFamily = "Noto Sans SC";

        static ImageGenerationService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
            RegisterEmbeddedFonts();
        }

        /// <summary>
        /// Loads NotoSansSC and NotoSansJP from embedded resources and registers them with
        /// QuestPDF. NotoSansSC is the primary font (covers Latin + Chinese characters and
        /// Japanese kanji). NotoSansJP is registered as well so QuestPDF's automatic glyph
        /// fallback can resolve hiragana and katakana that are absent in NotoSansSC.
        /// </summary>
        private static void RegisterEmbeddedFonts()
        {
            // RegisterFontFromEmbeddedResource searches all loaded assemblies by resource name.
            // NotoSansSC: Latin + Simplified Chinese + Japanese kanji (shared Unicode block).
            // NotoSansJP: hiragana + katakana + additional Japanese glyphs not in NotoSansSC.
            // QuestPDF automatically falls back across all registered fonts for missing glyphs.
            FontManager.RegisterFontFromEmbeddedResource("ResumeSpy.Infrastructure.Fonts.NotoSansSC-Regular.ttf");
            FontManager.RegisterFontFromEmbeddedResource("ResumeSpy.Infrastructure.Fonts.NotoSansJP-Regular.ttf");
        }

        public ImageGenerationService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<ImageGenerationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _supabaseUrl = GetRequiredConfiguration(configuration, "Supabase:Url", "Supabase URL not configured").TrimEnd('/');
            _serviceRoleKey = GetRequiredConfiguration(configuration, "Supabase:ServiceRoleKey", "Supabase service role key not configured");
            _storageBucket = configuration["Supabase:StorageBucket"]?.Trim() ?? "resume-thumbnails";
        }

        public async Task<string> GenerateThumbnailAsync(string text, string uniqueIdentifier)
        {
            // Portrait 3:4 — matches the dossier card aspect-ratio in the UI
            const float width  = 360;
            const float height = 480;

            var plainText = Markdown.ToPlainText(text ?? string.Empty);
            var sanitizedText = plainText.Length > 600 ? plainText[..600] + "…" : plainText;
            if (string.IsNullOrWhiteSpace(sanitizedText)) sanitizedText = "Resume";

            // NotoSansSC covers Latin + Chinese characters + Japanese kanji.
            // QuestPDF automatically falls back to NotoSansJP (also registered) for
            // hiragana and katakana glyphs that are not present in NotoSansSC.
            var defaultTextStyle = TextStyle.Default
                .FontFamily(PrimaryFontFamily)
                .FontSize(11)
                .FontColor("#323232")
                .LineHeight(1.5f);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(width, height);
                    page.Margin(0);
                    page.PageColor("#FFFFFF");
                    page.DefaultTextStyle(defaultTextStyle);

                    page.Content().Padding(20).AlignTop().Text(sanitizedText);
                });
            });

            byte[] imageBytes;

            try
            {
                imageBytes = document.GenerateImages().First();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to render resume thumbnail using QuestPDF. Ensure required fonts are available.", ex);
            }

            var fileName = $"{uniqueIdentifier}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
            var uploadUrl = BuildStorageObjectUrl(fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl)
            {
                Content = new ByteArrayContent(imageBytes)
            };

            request.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            request.Headers.Add("apikey", _serviceRoleKey);
            request.Headers.Add("x-upsert", "true");

            using var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to upload thumbnail to Supabase Storage. Status: {(int)response.StatusCode}, Body: {responseBody}");
            }

            return BuildPublicObjectUrl(fileName);
        }

        private static string GetRequiredConfiguration(IConfiguration configuration, string key, string errorMessage)
        {
            var value = configuration[key];
            return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException(errorMessage);
        }

        private string BuildStorageObjectUrl(string objectPath)
        {
            return $"{_supabaseUrl}/storage/v1/object/{_storageBucket}/{Uri.EscapeDataString(objectPath)}";
        }

        private string BuildPublicObjectUrl(string objectPath)
        {
            return $"{_supabaseUrl}/storage/v1/object/public/{_storageBucket}/{Uri.EscapeDataString(objectPath)}";
        }

        private string? TryExtractObjectPath(string imagePath)
        {
            if (!Uri.TryCreate(imagePath, UriKind.Absolute, out var uri))
            {
                return null;
            }

            var publicPrefix = $"/storage/v1/object/public/{_storageBucket}/";
            var objectPrefix = $"/storage/v1/object/{_storageBucket}/";

            if (uri.AbsolutePath.StartsWith(publicPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(uri.AbsolutePath[publicPrefix.Length..]);
            }

            if (uri.AbsolutePath.StartsWith(objectPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(uri.AbsolutePath[objectPrefix.Length..]);
            }

            return null;
        }

        public Task DeleteThumbnailAsync(string? imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return Task.CompletedTask;
            }

            return DeleteThumbnailInternalAsync(imagePath);
        }

        private async Task DeleteThumbnailInternalAsync(string imagePath)
        {
            var objectPath = TryExtractObjectPath(imagePath);

            if (string.IsNullOrWhiteSpace(objectPath))
            {
                return;
            }

            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Delete, BuildStorageObjectUrl(objectPath));
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
                request.Headers.Add("apikey", _serviceRoleKey);

                using var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning(
                    "Failed to delete thumbnail from Supabase Storage. Path: {ImagePath}, Status: {StatusCode}, Body: {ResponseBody}",
                    imagePath,
                    (int)response.StatusCode,
                    responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete thumbnail from Supabase Storage. Path: {ImagePath}", imagePath);
            }
        }
    }
}
