using ResumeSpy.Infrastructure.Services;
using System.Text;
using Xunit;

namespace ResumeSpy.Tests.Services;

/// <summary>
/// Unit tests for ResumeImportService.DecodeText — the multi-encoding text detector
/// that enables importing resumes in Chinese, Japanese, Korean, and other languages.
/// </summary>
public class ResumeImportEncodingTests
{
    public ResumeImportEncodingTests()
    {
        // Register code-page encodings (GBK, Shift-JIS, …) for the test process.
        // Safe to call multiple times; subsequent calls are no-ops.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    // ── UTF-8 (the common case) ──────────────────────────────────────────────

    [Fact]
    public void DecodeText_EmptyBytes_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, ResumeImportService.DecodeText([]));
    }

    [Fact]
    public void DecodeText_Utf8NoBom_DecodesCorrectly()
    {
        // Modern editors save Chinese/Japanese as UTF-8 without BOM.
        const string text = "Hello, 世界! こんにちは!";
        var bytes = new UTF8Encoding(false).GetBytes(text);

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    [Fact]
    public void DecodeText_Utf8WithBom_StripsAndDecodesCorrectly()
    {
        const string text = "Résumé – 简历 – 履歴書";
        var utf8Bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = utf8Bom.GetBytes(text); // starts with EF BB BF

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    [Fact]
    public void DecodeText_Ascii_DecodesCorrectly()
    {
        const string text = "John Doe\nSoftware Engineer";
        var bytes = Encoding.ASCII.GetBytes(text);

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    // ── UTF-16 (Windows Notepad default in older Windows) ────────────────────

    [Fact]
    public void DecodeText_Utf16LeBom_DecodesCorrectly()
    {
        const string text = "こんにちは世界";
        // Encoding.Unicode == UTF-16 LE; GetBytes includes the BOM preamble when
        // using the static property but NOT when constructing via new UnicodeEncoding().
        // We build the expected byte layout explicitly.
        var preamble = Encoding.Unicode.GetPreamble(); // FF FE
        var body = Encoding.Unicode.GetBytes(text);
        var bytes = preamble.Concat(body).ToArray();

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    [Fact]
    public void DecodeText_Utf16BeBom_DecodesCorrectly()
    {
        const string text = "안녕하세요";
        var preamble = Encoding.BigEndianUnicode.GetPreamble(); // FE FF
        var body = Encoding.BigEndianUnicode.GetBytes(text);
        var bytes = preamble.Concat(body).ToArray();

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    // ── Legacy CJK encodings ─────────────────────────────────────────────────

    [Fact]
    public void DecodeText_Gb18030_DecodesSimplifiedChinese()
    {
        // GB18030 is a superset of GBK / GB2312 — the dominant Chinese encoding
        // used by Windows tools before UTF-8 became widespread.
        const string text = "你好，我的名字是张伟。";
        var gb = Encoding.GetEncoding("GB18030");
        var bytes = gb.GetBytes(text);

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    [Fact]
    public void DecodeText_ShiftJis_DecodesJapanese()
    {
        // Shift-JIS is the legacy Japanese encoding produced by many Windows and
        // macOS apps when a Japanese locale is active.
        const string text = "田中太郎のレジュメ";
        var sjis = Encoding.GetEncoding("shift_jis");
        var bytes = sjis.GetBytes(text);

        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }

    // ── Correctness: UTF-8 is preferred over CJK when both would match ───────

    [Fact]
    public void DecodeText_ChineseUtf8_DoesNotMisdetectAsGb18030()
    {
        // A UTF-8 file with Chinese characters must be decoded as UTF-8, not GB18030,
        // because the byte sequences overlap but have different interpretations.
        const string text = "软件工程师";
        var bytes = new UTF8Encoding(false).GetBytes(text);

        // The result must still equal the original Chinese string.
        Assert.Equal(text, ResumeImportService.DecodeText(bytes));
    }
}
