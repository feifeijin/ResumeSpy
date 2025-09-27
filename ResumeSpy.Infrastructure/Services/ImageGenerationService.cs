using Markdig;
using Microsoft.Extensions.Hosting;
using ResumeSpy.Core.Interfaces.IServices;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Services
{
    public class ImageGenerationService : IImageGenerationService
    {
        private readonly IHostEnvironment _hostEnvironment;
        private const string ImageDirectory = "images/resumes";

        public ImageGenerationService(IHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public async Task<string> GenerateThumbnailAsync(string text, string uniqueIdentifier)
        {
            // 1. Define image properties
            int width = 400;
            int height = 300;
            var backgroundColor = Color.FromRgb(240, 240, 240); // Light gray
            var textColor = Color.FromRgb(50, 50, 50);       // Dark gray

            // 2. Convert Markdown to plain text and sanitize
            var plainText = Markdown.ToPlainText(text);
            var sanitizedText = plainText.Length > 150 ? plainText.Substring(0, 150) + "..." : plainText;

            // 3. Create the image
            using (var image = new Image<Rgba32>(width, height))
            {
                // 4. Load a font
                var fontCollection = new FontCollection();
                FontFamily fontFamily;
                try
                {
                    // Attempt to load a system font. This is more portable.
                    if (SystemFonts.TryGet("Arial", out var ff))
                    {
                        fontFamily = ff;
                    }
                    else if (SystemFonts.TryGet("Helvetica", out ff))
                    {
                        fontFamily = ff;
                    }
                    else if (SystemFonts.Families.Any())
                    {
                        fontFamily = SystemFonts.Families.First();
                    }
                    else
                    {
                        throw new Exception("No suitable system fonts found.");
                    }
                }
                catch (Exception ex)
                {
                    // In a real app, you might bundle a .ttf font file with your application
                    // and load it here as a fallback.
                    throw new Exception("Font loading failed. Ensure a common font like Arial or Helvetica is available, or bundle a font with the application.", ex);
                }
                
                var font = fontFamily.CreateFont(16, FontStyle.Regular);

                // 5. Fill background and draw text
                image.Mutate(ctx =>
                {
                    ctx.Fill(backgroundColor);

                    var textOptions = new RichTextOptions(font)
                    {
                        Origin = new PointF(10, 10),
                        WrappingLength = width - 20, // Wrap text within image bounds
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Top
                    };

                    ctx.DrawText(textOptions, sanitizedText, textColor);
                });

                // 6. Define file path and save the image
                var fileName = $"{uniqueIdentifier}_{DateTime.UtcNow:yyyyMMddHHmmss}.png";
                // Note: IHostEnvironment doesn't have WebRootPath. We need to construct it.
                // Assuming the UI project runs from the root of the workspace or similar.
                // A better approach is to have this path configured in appsettings.json
                var contentRootPath = _hostEnvironment.ContentRootPath;
                var webRootPath = Path.Combine(contentRootPath, "wwwroot"); // Common practice for web apps
                var uploadsFolderPath = Path.Combine(webRootPath, ImageDirectory);

                if (!Directory.Exists(uploadsFolderPath))
                {
                    Directory.CreateDirectory(uploadsFolderPath);
                }

                var filePath = Path.Combine(uploadsFolderPath, fileName);
                await image.SaveAsPngAsync(filePath);

                // 7. Return the relative path for web access
                return $"/{ImageDirectory}/{fileName}";
            }
        }
    }
}
