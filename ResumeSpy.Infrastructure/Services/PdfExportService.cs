using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ResumeSpy.Core.Interfaces.IServices;
using SixLabors.Fonts;
using System.Text;

namespace ResumeSpy.Infrastructure.Services
{
    public class PdfExportService : IPdfExportService
    {
        private static readonly object FontLock = new();
        private static string? _fontFamily;

        static PdfExportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
        }

        public Task<byte[]> GeneratePdfAsync(string content, string title)
        {
            var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
            var document = Markdown.Parse(content ?? string.Empty, pipeline);
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
                        foreach (var block in document)
                        {
                            RenderBlock(col, block);
                        }
                    });

                    page.Footer().AlignRight().Text(t =>
                    {
                        t.Span(title).FontSize(8).FontColor("#aaaaaa");
                        t.Span(" — ").FontSize(8).FontColor("#aaaaaa");
                        t.CurrentPageNumber().FontSize(8).FontColor("#aaaaaa");
                    });
                });
            });

            var bytes = pdfDocument.GeneratePdf();
            return Task.FromResult(bytes);
        }

        private static void RenderBlock(ColumnDescriptor col, Block block)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    var headingText = GetInlineText(heading.Inline);
                    col.Item().PaddingTop(heading.Level == 1 ? 0 : 10).PaddingBottom(2).Text(t =>
                    {
                        var span = t.Span(headingText);
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

                case ParagraphBlock para:
                    var paraText = GetInlineText(para.Inline);
                    if (!string.IsNullOrWhiteSpace(paraText))
                        col.Item().PaddingVertical(2).Text(paraText).FontSize(10);
                    break;

                case ListBlock list:
                    col.Item().PaddingTop(2).Column(listCol =>
                    {
                        foreach (var item in list)
                        {
                            if (item is ListItemBlock listItem)
                            {
                                foreach (var innerBlock in listItem)
                                {
                                    if (innerBlock is ParagraphBlock innerPara)
                                    {
                                        var itemText = GetInlineText(innerPara.Inline);
                                        listCol.Item().PaddingBottom(1).Row(row =>
                                        {
                                            row.ConstantItem(14).PaddingTop(1).Text("•").FontSize(9).FontColor("#555555");
                                            row.RelativeItem().Text(itemText).FontSize(10);
                                        });
                                    }
                                }
                            }
                        }
                    });
                    col.Item().PaddingBottom(2);
                    break;

                case ThematicBreakBlock:
                    col.Item().PaddingVertical(6).LineHorizontal(0.5f).LineColor("#dddddd");
                    break;

                case ContainerBlock container:
                    foreach (var inner in container)
                        RenderBlock(col, inner);
                    break;
            }
        }

        private static string GetInlineText(ContainerInline? inline)
        {
            if (inline == null) return string.Empty;
            var sb = new StringBuilder();
            foreach (var item in inline)
            {
                sb.Append(item switch
                {
                    LiteralInline literal => literal.Content.ToString(),
                    EmphasisInline emphasis => GetInlineText(emphasis),
                    LinkInline link => GetInlineText(link),
                    CodeInline code => code.Content,
                    LineBreakInline => " ",
                    _ => string.Empty
                });
            }
            return sb.ToString();
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

                return null;
            }
        }
    }
}
