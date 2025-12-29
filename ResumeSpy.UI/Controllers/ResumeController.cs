using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Middlewares;
using System.Security.Claims;
using X.PagedList;


namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly ILogger<ResumeController> _logger;
        private readonly IResumeService _resumeService;
        private readonly IResumeManagementService _resumeManagementService;
        private readonly IGuestSessionService _guestSessionService;
        private readonly IMemoryCache _memoryCache;

        public ResumeController(
            ILogger<ResumeController> logger, 
            IResumeService resumeService, 
            IResumeManagementService resumeManagementService,
            IGuestSessionService guestSessionService,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _resumeService = resumeService;
            _resumeManagementService = resumeManagementService;
            _guestSessionService = guestSessionService;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeViewModel>>> GetResumes()
        {
            var resumes = (await _resumeService.GetResumes()).ToList().OrderByDescending(r => r.EntryDate);
            return Ok(resumes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResumeViewModel>> GetResume(string id)
        {
            try
            {
                var resume = await _resumeService.GetResume(id);
                return Ok(resume);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost]
        public async Task<ActionResult<ResumeViewModel>> CreateResume([FromBody] ResumeViewModel resume)
        {
            try
            {
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var guestSessionId = HttpContext.GetGuestSessionId();
                
                // Authenticated users get full access
                if (!string.IsNullOrEmpty(userId))
                {
                    resume.IsGuest = false;
                    resume.UserId = userId;
                    resume.GuestSessionId = null;
                    resume.ExpiresAt = null;
                    var createdResume = await _resumeService.Create(resume);
                    return CreatedAtAction(nameof(GetResume), new { id = createdResume.Id }, createdResume);
                }

                // Guest flow: enforce limit
                if (guestSessionId.HasValue)
                {
                    var hasReachedLimit = await _guestSessionService.HasReachedResumeLimitAsync(guestSessionId.Value);
                    if (hasReachedLimit)
                    {
                        return StatusCode(403, new { error = "Guest resume limit reached. Please register to create more resumes." });
                    }

                    resume.IsGuest = true;
                    resume.GuestSessionId = guestSessionId.Value;
                    resume.CreatedIpAddress = HttpContext.GetGuestIpAddress();
                    resume.ExpiresAt = DateTime.UtcNow.AddDays(30);

                    var createdResume = await _resumeService.Create(resume);
                    await _guestSessionService.IncrementResumeCountAsync(guestSessionId.Value);

                    _logger.LogInformation($"Guest resume created: {createdResume.Id}");
                    return CreatedAtAction(nameof(GetResume), new { id = createdResume.Id }, createdResume);
                }

                // Should not happen because middleware auto-creates guest session for anonymous users
                return Unauthorized("Guest session not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error creating resume: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResumeViewModel>> UpdateResume(string id, [FromBody] ResumeViewModel updatedResume)
        {
            try
            {
                updatedResume.Id = id; // Ensure the ID matches the route parameter
                await _resumeService.Update(updatedResume);
                return Ok(updatedResume);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPatch("{id}/title")]
        public async Task<ActionResult<ResumeViewModel>> UpdateResumeName(string id, [FromBody] string title)
        {
            try
            {
                var existingResume = await _resumeService.GetResume(id);
                existingResume.Title = title;
                await _resumeService.Update(existingResume);
                return Ok(existingResume);
            }
            catch (Exception)
            {
                return NotFound();
            }
        }

        [HttpPost("{id}/clone")]
        public async Task<ActionResult<ResumeViewModel>> CloneResume(string id)
        {
            try
            {
                var clonedResume = await _resumeManagementService.CloneResumeAsync(id);
                return CreatedAtAction(nameof(GetResume), new { id = clonedResume.Id }, clonedResume);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cloning resume with ID: {ResumeId}", id);
                return StatusCode(500, "An error occurred while cloning the resume");
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteResume(string id)
        {
            try
            {
                await _resumeService.Delete(id);
                return NoContent();
            }
            catch (Exception)
            {
                return NotFound();
            }
        }
    }
}