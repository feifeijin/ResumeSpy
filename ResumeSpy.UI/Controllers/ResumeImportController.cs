using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Filters;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting("ai")]
    [ServiceFilter(typeof(AiAccessFilter))]
    public class ResumeImportController : ControllerBase
    {
        // Reasonable upper-bound for file extraction + AI processing, even for large
        // multi-page resumes in CJK languages (which produce more tokens than Latin text).
        private static readonly TimeSpan ImportTimeout = TimeSpan.FromSeconds(120);

        private readonly IResumeImportService _importService;
        private readonly ILogger<ResumeImportController> _logger;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".doc", ".txt", ".md" };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public ResumeImportController(IResumeImportService importService, ILogger<ResumeImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        /// <summary>
        /// Accepts a resume file (PDF, DOCX, DOC, TXT, MD) and returns the content
        /// converted to structured Markdown by the AI provider.
        /// Supports files in any language including Chinese, Japanese, Korean, etc.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult> Import(IFormFile file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { error = "File exceeds the 10 MB limit." });

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { error = $"Unsupported file type '{ext}'. Allowed: PDF, DOCX, DOC, TXT, MD." });

            // Link the client-disconnect token with our own deadline so that long-running
            // AI calls are cancelled promptly regardless of which signal fires first.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(ImportTimeout);

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _importService.ImportAsync(stream, ext, cts.Token);

                return Ok(new
                {
                    markdown = result.Markdown,
                    suggestedTitle = result.SuggestedTitle,
                });
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Our deadline fired before the client disconnected.
                _logger.LogWarning(
                    "Resume import timed out after {Seconds}s for file '{Name}'.",
                    (int)ImportTimeout.TotalSeconds, file.FileName);
                return StatusCode(504, new { error = $"The request timed out after {(int)ImportTimeout.TotalSeconds} seconds. Please try again or upload a smaller file." });
            }
            catch (NotSupportedException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Import failed: {Message}", ex.Message);
                return UnprocessableEntity(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during resume import");
                return StatusCode(500, new { error = "An error occurred while processing the file." });
            }
        }
    }
}
