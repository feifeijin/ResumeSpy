using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using ResumeSpy.Core.Entities.Export;
using ResumeSpy.Core.Interfaces.IServices;
using System.Text;

namespace ResumeSpy.Infrastructure.Services
{
    public class ResumeParser : IResumeParser
    {
        private static readonly MarkdownPipeline Pipeline =
            new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

        public ResumeDocument Parse(string markdown, string title)
        {
            var document = Markdown.Parse(markdown ?? string.Empty, Pipeline);
            var blocks = new List<ResumeBlock>();

            foreach (var block in document)
                ConvertBlock(block, blocks);

            return new ResumeDocument { Title = title, Blocks = blocks };
        }

        private static void ConvertBlock(Block block, List<ResumeBlock> blocks)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    blocks.Add(new ResumeHeadingBlock
                    {
                        Level = heading.Level,
                        Text = GetInlineText(heading.Inline)
                    });
                    break;

                case ParagraphBlock para:
                    var text = GetInlineText(para.Inline);
                    if (!string.IsNullOrWhiteSpace(text))
                        blocks.Add(new ResumeParagraphBlock { Text = text });
                    break;

                case ListBlock list:
                    var items = new List<string>();
                    foreach (var item in list)
                    {
                        if (item is ListItemBlock listItem)
                        {
                            foreach (var inner in listItem)
                            {
                                if (inner is ParagraphBlock innerPara)
                                    items.Add(GetInlineText(innerPara.Inline));
                            }
                        }
                    }
                    blocks.Add(new ResumeBulletListBlock { Items = items });
                    break;

                case ThematicBreakBlock:
                    blocks.Add(new ResumeDividerBlock());
                    break;

                case ContainerBlock container:
                    foreach (var inner in container)
                        ConvertBlock(inner, blocks);
                    break;
            }
        }

        internal static string GetInlineText(ContainerInline? inline)
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
    }
}
