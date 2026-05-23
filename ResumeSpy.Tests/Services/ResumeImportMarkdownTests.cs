using ResumeSpy.Infrastructure.Services;
using System.Reflection;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class ResumeImportMarkdownTests
{
    // ── ExtractMarkdownTitle ─────────────────────────────────────────────────

    private static string InvokeExtractMarkdownTitle(string markdown)
    {
        var method = typeof(ResumeImportService)
            .GetMethod("ExtractMarkdownTitle", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [markdown])!;
    }

    [Fact]
    public void ExtractMarkdownTitle_WithH1Heading_ReturnsHeadingText()
    {
        const string markdown = "# John Doe\n\n## Experience\n- Software Engineer";
        Assert.Equal("John Doe", InvokeExtractMarkdownTitle(markdown));
    }

    [Fact]
    public void ExtractMarkdownTitle_WithNoH1Heading_ReturnsDefault()
    {
        const string markdown = "## Experience\n- Software Engineer";
        Assert.Equal("Imported Resume", InvokeExtractMarkdownTitle(markdown));
    }

    [Fact]
    public void ExtractMarkdownTitle_WithMultipleH1Headings_ReturnsFirst()
    {
        const string markdown = "# Alice\n# Bob\n## Skills";
        Assert.Equal("Alice", InvokeExtractMarkdownTitle(markdown));
    }

    [Fact]
    public void ExtractMarkdownTitle_EmptyString_ReturnsDefault()
    {
        Assert.Equal("Imported Resume", InvokeExtractMarkdownTitle(string.Empty));
    }

    [Fact]
    public void ExtractMarkdownTitle_H1WithLeadingWhitespace_ReturnsHeadingText()
    {
        const string markdown = "   # Jane Smith\n\n## Education";
        Assert.Equal("Jane Smith", InvokeExtractMarkdownTitle(markdown));
    }
}
