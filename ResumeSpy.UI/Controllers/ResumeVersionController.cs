using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeVersionController : ControllerBase
    {
        private readonly ILogger<ResumeVersionController> _logger;
        private readonly IResumeVersionService _versionService;

        public ResumeVersionController(ILogger<ResumeVersionController> logger, IResumeVersionService versionService)
        {
            _logger = logger;
            _versionService = versionService;
        }

        /// <summary>
        /// GET api/resumeversion?resumeDetailId={id}
        /// Returns all saved versions for a resume detail, newest first.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ResumeVersionViewModel>>> GetVersionsAsync([FromQuery] string resumeDetailId)
        {
            try
            {
                var versions = await _versionService.GetVersionsAsync(resumeDetailId);
                return Ok(versions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching versions for {ResumeDetailId}", resumeDetailId);
                return StatusCode(500, "An error occurred while fetching versions.");
            }
        }

        /// <summary>
        /// POST api/resumeversion
        /// Saves a new snapshot. Body: { resumeDetailId, content, label? }
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ResumeVersionViewModel>> SaveVersionAsync([FromBody] SaveVersionRequest request)
        {
            try
            {
                var version = await _versionService.SaveVersionAsync(request.ResumeDetailId, request.Content, request.Label);
                return Ok(version);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving version for {ResumeDetailId}", request.ResumeDetailId);
                return StatusCode(500, "An error occurred while saving the version.");
            }
        }

        /// <summary>
        /// GET api/resumeversion/{id}/content
        /// Returns the full content of a specific version (for restore / diff).
        /// </summary>
        [HttpGet("{id:guid}/content")]
        public async Task<ActionResult<string>> GetVersionContentAsync(Guid id)
        {
            try
            {
                var content = await _versionService.GetVersionContentAsync(id);
                return Ok(content);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching content for version {Id}", id);
                return StatusCode(500, "An error occurred while fetching version content.");
            }
        }

        /// <summary>
        /// DELETE api/resumeversion/{id}
        /// Deletes a specific version snapshot.
        /// </summary>
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteVersionAsync(Guid id)
        {
            try
            {
                await _versionService.DeleteVersionAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting version {Id}", id);
                return StatusCode(500, "An error occurred while deleting the version.");
            }
        }
    }

    public class SaveVersionRequest
    {
        public required string ResumeDetailId { get; set; }
        public required string Content { get; set; }
        public string? Label { get; set; }
    }
}
