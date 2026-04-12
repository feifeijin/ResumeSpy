using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeImportController : ControllerBase
    {
        private readonly IResumeImportService _importService;
        private readonly ILogger<ResumeImportController> _logger;

        private static readonly HashSet<string> AllowedExtensions =
            new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".doc", ".txt" };

        private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

        public ResumeImportController(IResumeImportService importService, ILogger<ResumeImportController> logger)
        {
            _importService = importService;
            _logger = logger;
        }

        /// <summary>
        /// Accepts a resume file (PDF, DOCX, DOC, TXT) and returns the content
        /// converted to structured Markdown by the AI provider.
        /// </summary>
        [HttpPost]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<ActionResult> Import(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file provided." });

            if (file.Length > MaxFileSizeBytes)
                return BadRequest(new { error = "File exceeds the 10 MB limit." });

            var ext = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(ext))
                return BadRequest(new { error = $"Unsupported file type '{ext}'. Allowed: PDF, DOCX, DOC, TXT." });

            try
            {
                using var stream = file.OpenReadStream();
                var result = await _importService.ImportAsync(stream, ext);

                return Ok(new
                {
                    markdown = result.Markdown,
                    suggestedTitle = result.SuggestedTitle,
                });
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
