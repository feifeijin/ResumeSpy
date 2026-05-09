namespace ResumeSpy.Core.Interfaces.IServices
{
    public interface IResumeImportService
    {
        /// <summary>
        /// Extracts text from a resume file (PDF, DOCX, TXT) and converts it to
        /// structured Markdown using the AI provider.
        /// </summary>
        /// <param name="stream">File content stream</param>
        /// <param name="extension">Lowercase file extension, e.g. ".pdf"</param>
        /// <param name="cancellationToken">Propagated from the HTTP request; allows the caller to abort long-running AI calls</param>
        /// <returns>Markdown string and a suggested resume title</returns>
        Task<ResumeImportResult> ImportAsync(Stream stream, string extension, CancellationToken cancellationToken = default);
    }

    public record ResumeImportResult(string Markdown, string SuggestedTitle);
}
