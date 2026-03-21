using Markdig;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeSpy.Core.Interfaces.IServices;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class ImageGenerationService : IImageGenerationService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ImageGenerationService> _logger;
        private readonly string _supabaseUrl;
        private readonly string _serviceRoleKey;
        private readonly string _storageBucket;
        private static readonly object FontRegistrationLock = new();
        private static string? _questPdfFontFamily;

        static ImageGenerationService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
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
            const float width = 400;
            const float height = 300;

            var plainText = Markdown.ToPlainText(text ?? string.Empty);
            var sanitizedText = plainText.Length > 300 ? plainText[..300] + "..." : plainText;

            if (string.IsNullOrWhiteSpace(sanitizedText))
            {
                sanitizedText = "Resume";
            }

            var fontFamily = ResolveQuestPdfFontFamily();
            var defaultTextStyle = TextStyle.Default.FontSize(14).FontColor("#323232").LineHeight(1.35f);

            if (!string.IsNullOrWhiteSpace(fontFamily))
            {
                defaultTextStyle = defaultTextStyle.FontFamily(fontFamily);
            }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(width, height);
                    page.Margin(0);
                    page.PageColor("#F0F0F0");
                    page.DefaultTextStyle(defaultTextStyle);

                    page.Content().Padding(20).AlignTop().Text(textComposer =>
                    {
                        textComposer.Span(sanitizedText);
                    });
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

        private string? ResolveQuestPdfFontFamily()
        {
            if (!string.IsNullOrWhiteSpace(_questPdfFontFamily))
            {
                return _questPdfFontFamily;
            }

            lock (FontRegistrationLock)
            {
                if (!string.IsNullOrWhiteSpace(_questPdfFontFamily))
                {
                    return _questPdfFontFamily;
                }

                var preferredFonts = new[]
                {
                    "PingFang SC",
                    "Hiragino Sans",
                    "Microsoft YaHei",
                    "Meiryo",
                    "Noto Sans CJK SC",
                    "Noto Sans CJK JP",
                    "Noto Sans",
                    "Arial Unicode MS",
                    "Arial",
                    "Helvetica"
                };

                foreach (var fontName in preferredFonts)
                {
                    if (SystemFonts.TryGet(fontName, out _))
                    {
                        _questPdfFontFamily = fontName;
                        return _questPdfFontFamily;
                    }
                }

                _questPdfFontFamily = null;
                return _questPdfFontFamily;
            }
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
