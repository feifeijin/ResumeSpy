using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Prompts;
using ResumeSpy.Infrastructure.Services.AI;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace ResumeSpy.Infrastructure.Services
{
    public class ResumeImportService : IResumeImportService
    {
        private readonly AIOrchestratorService _aiOrchestrator;
        private readonly ILogger<ResumeImportService> _logger;

        private static readonly HashSet<string> SupportedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".doc", ".txt", ".md" };

        public ResumeImportService(AIOrchestratorService aiOrchestrator, ILogger<ResumeImportService> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
        }

        public async Task<ResumeImportResult> ImportAsync(Stream stream, string extension, CancellationToken cancellationToken = default)
        {
            if (!SupportedExtensions.Contains(extension))
                throw new NotSupportedException($"File type '{extension}' is not supported.");

            var rawText = extension.ToLowerInvariant() switch
            {
                ".pdf"            => ExtractFromPdf(stream),
                ".docx" or ".doc" => ExtractFromDocx(stream),
                ".txt"  or ".md"  => await ExtractFromTxt(stream),
                _                 => throw new NotSupportedException($"File type '{extension}' is not supported.")
            };

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("No readable text found in the uploaded file.");

            _logger.LogInformation("Extracted {Chars} chars from {Ext} file. Sending to AI.", rawText.Length, extension);

            return await ConvertToMarkdownAsync(rawText, cancellationToken);
        }

        private static string ExtractFromPdf(Stream stream)
        {
            using var pdf = PdfDocument.Open(stream);
            var sb = new StringBuilder();
            foreach (Page page in pdf.GetPages())
                sb.AppendLine(page.Text);
            return sb.ToString();
        }

        private static string ExtractFromDocx(Stream stream)
        {
            using var doc = WordprocessingDocument.Open(stream, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null) return string.Empty;

            var sb = new StringBuilder();
            foreach (var para in body.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                var text = para.InnerText.Trim();
                if (!string.IsNullOrEmpty(text))
                    sb.AppendLine(text);
            }
            return sb.ToString();
        }

        private static async Task<string> ExtractFromTxt(Stream stream)
        {
            // Buffer the full stream so we can inspect the raw bytes for encoding detection.
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            var bytes = ms.ToArray();
            return DecodeText(bytes);
        }

        /// <summary>
        /// Detects the encoding of a raw byte array and returns the decoded string.
        /// Detection order:
        /// 1. BOM markers (UTF-8, UTF-16 LE/BE) — most reliable
        /// 2. Strict UTF-8 validation — covers the vast majority of modern files,
        ///    including those with Chinese/Japanese/Korean content saved as UTF-8
        /// 3. Language-aware CJK heuristic:
        ///    — Each candidate encoding is tried with a strict (exception) fallback so
        ///      only encodings that can fully decode the bytes are considered.
        ///    — GB18030 and Shift-JIS byte ranges overlap, so a tiebreaker counts
        ///      Hiragana + Katakana characters in the Shift-JIS decode: Japanese text
        ///      almost always contains these syllabic scripts, while Chinese text does not.
        ///    — Fallback priority: GB18030 → Shift-JIS → EUC-JP → EUC-KR
        /// 4. Permissive UTF-8 as the last resort (invalid bytes → U+FFFD)
        ///
        /// Requires <c>Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)</c>
        /// to be called once at application startup (see Program.cs).
        /// </summary>
        internal static string DecodeText(byte[] bytes)
        {
            if (bytes.Length == 0) return string.Empty;

            // ── BOM-based detection ──────────────────────────────────────────────
            // UTF-8 BOM: EF BB BF
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            // UTF-16 LE BOM: FF FE
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            // UTF-16 BE BOM: FE FF
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            // ── Strict UTF-8 (no BOM) ────────────────────────────────────────────
            // This covers modern Chinese/Japanese/Korean files saved as UTF-8.
            try
            {
                return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                    .GetString(bytes);
            }
            catch (DecoderFallbackException) { /* not valid UTF-8 – fall through */ }

            // ── Legacy CJK encoding detection ────────────────────────────────────
            // GB18030 and Shift-JIS byte ranges overlap: a Shift-JIS file may decode
            // without errors under GB18030, but with completely different characters.
            // We resolve the ambiguity by counting Hiragana / Katakana syllabics in
            // the Shift-JIS result — Japanese text almost always contains them, Chinese
            // text does not, so the winning encoding is unambiguous in practice.
            var gb   = TryStrictDecode(bytes, "GB18030");
            var sjis = TryStrictDecode(bytes, "shift_jis");

            if (sjis != null && gb != null)
            {
                // Both can decode the bytes — decide by Japanese syllabic content.
                if (CountJapaneseSyllabics(sjis) > CountJapaneseSyllabics(gb))
                    return sjis;
                // Otherwise Chinese (GB18030) is the better fit.
                return gb;
            }

            if (gb   != null) return gb;
            if (sjis != null) return sjis;

            // EUC encodings as a last resort before the permissive fallback.
            var eucJp = TryStrictDecode(bytes, "euc-jp");
            if (eucJp != null) return eucJp;

            var eucKr = TryStrictDecode(bytes, "euc-kr");
            if (eucKr != null) return eucKr;

            // ── Permissive UTF-8 fallback ────────────────────────────────────────
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Attempts to decode <paramref name="bytes"/> using <paramref name="encodingName"/>
        /// with a strict error fallback. Returns <c>null</c> if the encoding is not
        /// registered or if any byte sequence is invalid for that encoding.
        /// </summary>
        private static string? TryStrictDecode(byte[] bytes, string encodingName)
        {
            try
            {
                var enc = Encoding.GetEncoding(
                    encodingName,
                    new EncoderReplacementFallback("?"),
                    new DecoderExceptionFallback());
                return enc.GetString(bytes);
            }
            catch (DecoderFallbackException) { return null; }
            catch (ArgumentException)        { return null; } // encoding not registered
        }

        /// <summary>
        /// Counts Hiragana (U+3040–U+309F) and full-width Katakana (U+30A0–U+30FF)
        /// characters in <paramref name="text"/>. A non-zero count is a strong signal
        /// that the text is Japanese and should be decoded with Shift-JIS.
        /// </summary>
        private static int CountJapaneseSyllabics(string text) =>
            text.Count(c => (c >= '぀' && c <= 'ゟ') ||
                            (c >= '゠' && c <= 'ヿ'));

        private async Task<ResumeImportResult> ConvertToMarkdownAsync(string rawText, CancellationToken cancellationToken)
        {
            var response = await _aiOrchestrator.ExecuteTextGenerationAsync(new AIRequest
            {
                Prompt = ImportPrompts.BuildPrompt(rawText),
                SystemMessage = ImportPrompts.SystemMessage,
                Temperature = 0.2,
                MaxTokens = 4096,
            }, useCache: false, cancellationToken: cancellationToken);

            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
                throw new InvalidOperationException($"AI conversion failed: {response.ErrorMessage}");

            var content = response.Content.Trim();

            // Extract suggested title from the last line
            var lines = content.Split('\n');
            var titleLine = lines.LastOrDefault(l => l.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase));
            string suggestedTitle = "Imported Resume";
            string markdown = content;

            if (titleLine != null)
            {
                suggestedTitle = titleLine["TITLE:".Length..].Trim();
                markdown = string.Join('\n', lines.Where(l => !l.StartsWith("TITLE:", StringComparison.OrdinalIgnoreCase))).TrimEnd();
            }

            _logger.LogInformation("AI import conversion succeeded. Suggested title: {Title}", suggestedTitle);
            return new ResumeImportResult(markdown, suggestedTitle);
        }
    }
}
