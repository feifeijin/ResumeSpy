using ResumeSpy.Infrastructure.Services;
using Xunit;

namespace ResumeSpy.Tests.Services;

public class PdfExportServiceTests
{
    private readonly PdfExportService _service = new();

    [Fact]
    public async Task GeneratePdfAsync_WithChineseContent_ReturnsPdfBytes()
    {
        // Purpose: verify Chinese (Simplified) text does not cause an exception and
        // produces a valid PDF byte array (non-empty, starts with the %PDF header).
        var content = """
            # 个人简历

            ## 基本信息

            - 姓名：张三
            - 邮箱：zhangsan@example.com

            ## 工作经验

            在某科技公司担任高级软件工程师，负责后端系统设计与开发。
            """;

        var result = await _service.GeneratePdfAsync(content, "个人简历");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        // All PDFs start with the magic bytes %PDF
        Assert.Equal("%PDF"u8.ToArray(), result[..4]);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithJapaneseContent_ReturnsPdfBytes()
    {
        // Purpose: verify Japanese text does not cause an exception and produces a
        // valid PDF byte array (non-empty, starts with the %PDF header).
        var content = """
            # 履歴書

            ## 基本情報

            - 氏名：山田太郎
            - メール：yamada@example.com

            ## 職務経歴

            大手IT企業でシニアソフトウェアエンジニアとして、バックエンドシステムの設計・開発に従事。
            """;

        var result = await _service.GeneratePdfAsync(content, "履歴書");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal("%PDF"u8.ToArray(), result[..4]);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithEnglishContent_ReturnsPdfBytes()
    {
        // Purpose: verify Latin-script content is unaffected by the CJK font change.
        var content = """
            # Resume

            ## Experience

            - Senior Software Engineer at Acme Corp
            - Led backend architecture for distributed systems
            """;

        var result = await _service.GeneratePdfAsync(content, "Resume");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal("%PDF"u8.ToArray(), result[..4]);
    }

    [Fact]
    public async Task GeneratePdfAsync_WithMixedCjkAndLatinContent_ReturnsPdfBytes()
    {
        // Purpose: verify mixed Chinese/Japanese/English content (common in real resumes)
        // renders without exceptions.
        var content = """
            # 張 Taro / 张三

            Senior Engineer — ソフトウェアエンジニア / 软件工程师

            ## Skills

            - C#, .NET, TypeScript
            - 日本語（ビジネスレベル）
            - 中文（母语）
            """;

        var result = await _service.GeneratePdfAsync(content, "Mixed Resume");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Equal("%PDF"u8.ToArray(), result[..4]);
    }
}
