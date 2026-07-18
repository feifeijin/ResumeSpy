using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeSpy.Core.Entities.Export;
using ResumeSpy.Core.Interfaces.IServices;
using SixLabors.Fonts;
using System.Reflection;

namespace ResumeSpy.Infrastructure.Services.Exporters
{
    public class PdfExporter : IResumeExporter<byte[]>
    {
        private static readonly object FontLock = new();
        private static string? _fontFamily;

        static PdfExporter()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;

            // Register embedded CJK fonts so Chinese and Japanese glyphs always render
            // correctly regardless of which fonts are installed on the host OS.
            var assembly = Assembly.GetExecutingAssembly();
            RegisterEmbeddedFont(assembly, "ResumeSpy.Infrastructure.Fonts.NotoSansSC-Regular.ttf");
            RegisterEmbeddedFont(assembly, "ResumeSpy.Infrastructure.Fonts.NotoSansJP-Regular.ttf");
        }

        private static void RegisterEmbeddedFont(Assembly assembly, string resourceName)
        {
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null) return;
            FontManager.RegisterFont(stream);
        }

        public Task<byte[]> ExportAsync(ResumeDocument resume)
        {
            var fontFamily = ResolveFontFamily();

            var pdfDocument = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.MarginHorizontal(45);
                    page.MarginVertical(40);
                    page.DefaultTextStyle(style =>
                    {
                        var s = style.FontSize(10).LineHeight(1.5f).FontColor("#1a1a1a");
                        return !string.IsNullOrWhiteSpace(fontFamily) ? s.FontFamily(fontFamily) : s;
                    });

                    page.Content().Column(col =>
                    {
                        foreach (var block in resume.Blocks)
                            RenderBlock(col, block);
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span(resume.Title).FontSize(8).FontColor("#aaaaaa");
                        t.Span(" — ").FontSize(8).FontColor("#aaaaaa");
                        t.CurrentPageNumber().FontSize(8).FontColor("#aaaaaa");
                    });
                });
            });

            var bytes = pdfDocument.GeneratePdf();
            return Task.FromResult(bytes);
        }

        private static void RenderBlock(ColumnDescriptor col, ResumeBlock block)
        {
            switch (block)
            {
                case ResumeHeadingBlock heading:
                    col.Item().PaddingTop(heading.Level == 1 ? 0 : 10).PaddingBottom(2).Text(t =>
                    {
                        var span = t.Span(heading.Text);
                        switch (heading.Level)
                        {
                            case 1: span.FontSize(20).Bold().FontColor("#111111"); break;
                            case 2: span.FontSize(13).Bold().FontColor("#222222"); break;
                            default: span.FontSize(11).Bold().FontColor("#333333"); break;
                        }
                    });
                    if (heading.Level <= 2)
                        col.Item().PaddingBottom(4).LineHorizontal(heading.Level == 1 ? 1.5f : 0.5f)
                            .LineColor(heading.Level == 1 ? "#333333" : "#cccccc");
                    break;

                case ResumeParagraphBlock para:
                    col.Item().PaddingVertical(2).Text(para.Text).FontSize(10);
                    break;

                case ResumeBulletListBlock list:
                    col.Item().PaddingTop(2).Column(listCol =>
                    {
                        foreach (var item in list.Items)
                        {
                            listCol.Item().PaddingBottom(1).Row(row =>
                            {
                                row.ConstantItem(14).PaddingTop(1).Text("•").FontSize(9).FontColor("#555555");
                                row.RelativeItem().Text(item).FontSize(10);
                            });
                        }
                    });
                    col.Item().PaddingBottom(2);
                    break;

                case ResumeDividerBlock:
                    col.Item().PaddingVertical(6).LineHorizontal(0.5f).LineColor("#dddddd");
                    break;
            }
        }

        private static string? ResolveFontFamily()
        {
            if (!string.IsNullOrWhiteSpace(_fontFamily))
                return _fontFamily;

            lock (FontLock)
            {
                if (!string.IsNullOrWhiteSpace(_fontFamily))
                    return _fontFamily;

                var preferred = new[]
                {
                    "PingFang SC", "Hiragino Sans", "Microsoft YaHei",
                    "Meiryo", "Noto Sans CJK SC", "Noto Sans CJK JP",
                    "Noto Sans", "Arial Unicode MS", "Arial", "Helvetica"
                };

                foreach (var f in preferred)
                {
                    if (SystemFonts.TryGet(f, out _))
                    {
                        _fontFamily = f;
                        return _fontFamily;
                    }
                }

                // Fall back to the embedded Noto Sans SC font, which covers Simplified
                // Chinese and, combined with the registered Noto Sans JP fallback font,
                // also covers Japanese.
                _fontFamily = "Noto Sans SC";
                return _fontFamily;
            }
        }
    }
}
