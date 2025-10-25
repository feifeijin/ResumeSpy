using Markdig;
using Microsoft.Extensions.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeSpy.Core.Interfaces.IServices;
using SixLabors.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class ImageGenerationService : IImageGenerationService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private const string ImageDirectory = "images/resumes";
        private static readonly object FontRegistrationLock = new();
        private static string? _questPdfFontFamily;

        static ImageGenerationService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public ImageGenerationService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
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
            var contentRootPath = _hostEnvironment.ContentRootPath;
            var webRootPath = Path.Combine(contentRootPath, "wwwroot");
            var uploadsFolderPath = Path.Combine(webRootPath, ImageDirectory);

            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            var filePath = Path.Combine(uploadsFolderPath, fileName);
            await File.WriteAllBytesAsync(filePath, imageBytes);

            return $"/{ImageDirectory}/{fileName}";
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

            try
            {
                // Construct the full physical path from the relative web path
                var contentRootPath = _hostEnvironment.ContentRootPath;
                var webRootPath = Path.Combine(contentRootPath, "wwwroot");
                // Remove the leading slash from the imagePath to correctly combine paths
                var physicalPath = Path.Combine(webRootPath, imagePath.TrimStart('/'));

                if (File.Exists(physicalPath))
                {
                    File.Delete(physicalPath);
                }
            }
            catch (Exception)
            {
                // Log the exception in a real application
                // For now, we'll just ignore it to prevent crashing the operation
            }

            return Task.CompletedTask;
        }
    }
}
