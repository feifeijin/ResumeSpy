using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
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
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".doc", ".txt" };

        public ResumeImportService(AIOrchestratorService aiOrchestrator, ILogger<ResumeImportService> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
        }

        public async Task<ResumeImportResult> ImportAsync(Stream stream, string extension)
        {
            if (!SupportedExtensions.Contains(extension))
                throw new NotSupportedException($"File type '{extension}' is not supported.");

            var rawText = extension.ToLowerInvariant() switch
            {
                ".pdf"  => ExtractFromPdf(stream),
                ".docx" or ".doc" => ExtractFromDocx(stream),
                ".txt"  => await ExtractFromTxt(stream),
                _       => throw new NotSupportedException($"File type '{extension}' is not supported.")
            };

            if (string.IsNullOrWhiteSpace(rawText))
                throw new InvalidOperationException("No readable text found in the uploaded file.");

            _logger.LogInformation("Extracted {Chars} chars from {Ext} file. Sending to AI.", rawText.Length, extension);

            return await ConvertToMarkdownAsync(rawText);
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
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }

        private async Task<ResumeImportResult> ConvertToMarkdownAsync(string rawText)
        {
            const string systemMessage = """
                You are an expert resume formatter. Convert the provided resume text into clean, well-structured Markdown.

                Rules:
                - Use # for the candidate's full name at the top
                - Use ## for section headers: Summary, Experience, Education, Skills, etc.
                - Use ### for job titles / company names within Experience
                - Use bullet points (-) for responsibilities and achievements
                - Preserve ALL factual data: names, dates, companies, education, contact info
                - Format contact info (email, phone, LinkedIn) as a single line under the name
                - Do NOT add, invent, or infer any information not present in the original
                - Return ONLY the Markdown — no preamble, no explanation, no code fences
                - On the very last line, after a blank line, write: TITLE: <first name last name>'s Resume
                """;

            var prompt = $"""
                Convert the following resume text to Markdown:

                {rawText}
                """;

            var response = await _aiOrchestrator.ExecuteTextGenerationAsync(new AIRequest
            {
                Prompt = prompt,
                SystemMessage = systemMessage,
                Temperature = 0.2,
                MaxTokens = 4096,
            }, useCache: false);

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
