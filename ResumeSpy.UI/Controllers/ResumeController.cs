using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Exceptions;
using ResumeSpy.Core.Interfaces.IRepositories;
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
        private readonly IAnonymousUserService _anonymousUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _memoryCache;

        public ResumeController(
            ILogger<ResumeController> logger, 
            IResumeService resumeService, 
            IResumeManagementService resumeManagementService,
            IAnonymousUserService anonymousUserService,
            IUnitOfWork unitOfWork,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _resumeService = resumeService;
            _resumeManagementService = resumeManagementService;
            _anonymousUserService = anonymousUserService;
            _unitOfWork = unitOfWork;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeViewModel>>> GetResumes()
        {
            var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var anonymousUserId = HttpContext.GetAnonymousUserId();
            
            var resumes = (await _resumeService.GetResumes(userId, anonymousUserId))
                .OrderByDescending(r => r.EntryDate)
                .ToList();
            
            return Ok(resumes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ResumeViewModel>> GetResume(string id)
        {
            try
            {
                var resume = await _resumeService.GetResume(id);
                
                // Authorization check
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                
                bool isAuthorized = 
                    (!string.IsNullOrEmpty(userId) && resume.UserId == userId) ||
                    (anonymousUserId.HasValue && resume.AnonymousUserId == anonymousUserId);
                
                if (!isAuthorized)
                {
                    return Forbid();
                }
                
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
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                
                // Authenticated users get full access
                if (!string.IsNullOrEmpty(userId))
                {
                    resume.IsGuest = false;
                    resume.UserId = userId;
                    resume.AnonymousUserId = null;
                    resume.ExpiresAt = null;
                    var createdResume = await _resumeService.Create(resume);
                    return CreatedAtAction(nameof(GetResume), new { id = createdResume.Id }, createdResume);
                }

                // Anonymous flow: enforce limits
                if (anonymousUserId.HasValue)
                {
                    // Check per-user limit
                    var hasReachedLimit = await _anonymousUserService.HasReachedResumeLimitAsync(anonymousUserId.Value);
                    if (hasReachedLimit)
                    {
                        return StatusCode(403, new { error = "Resume limit reached. Please register to create more resumes." });
                    }

                    resume.IsGuest = true;
                    resume.AnonymousUserId = anonymousUserId.Value;
                    resume.ExpiresAt = DateTime.UtcNow.AddDays(30);

                    await _unitOfWork.BeginTransactionAsync();
                    ResumeViewModel createdResume;
                    try
                    {
                        var acquired = await _anonymousUserService.TryAcquireResumeSlotAsync(anonymousUserId.Value);
                        if (!acquired)
                        {
                            await _unitOfWork.RollbackTransactionAsync();
                            return StatusCode(403, new { error = "Resume limit reached. Please register to create more resumes." });
                        }

                        createdResume = await _resumeService.Create(resume);
                        await _unitOfWork.CommitTransactionAsync();
                    }
                    catch
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw;
                    }

                    _logger.LogInformation("Anonymous resume created: {ResumeId} from user {AnonymousUserId}", createdResume.Id, anonymousUserId.Value);
                    return CreatedAtAction(nameof(GetResume), new { id = createdResume.Id }, createdResume);
                }

                return Unauthorized("Anonymous user identity not found.");
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
                var existingResume = await _resumeService.GetResume(id);
                
                // Authorization check
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                
                bool isAuthorized = 
                    (!string.IsNullOrEmpty(userId) && existingResume.UserId == userId) ||
                    (anonymousUserId.HasValue && existingResume.AnonymousUserId == anonymousUserId);
                
                if (!isAuthorized)
                {
                    return Forbid();
                }
                
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
                
                // Authorization check
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                
                bool isAuthorized = 
                    (!string.IsNullOrEmpty(userId) && existingResume.UserId == userId) ||
                    (anonymousUserId.HasValue && existingResume.AnonymousUserId == anonymousUserId);
                
                if (!isAuthorized)
                {
                    return Forbid();
                }
                
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
                var existingResume = await _resumeService.GetResume(id);
                
                // Authorization check
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                
                bool isAuthorized = 
                    (!string.IsNullOrEmpty(userId) && existingResume.UserId == userId) ||
                    (anonymousUserId.HasValue && existingResume.AnonymousUserId == anonymousUserId);
                
                if (!isAuthorized)
                {
                    return Forbid();
                }
                
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
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var anonymousUserId = HttpContext.GetAnonymousUserId();

                // Use atomic deletion that properly handles anonymous user count decrement
                await _resumeManagementService.DeleteResumeAtomicAsync(id, userId, anonymousUserId);

                _logger.LogInformation($"Resume {id} deleted successfully");
                return NoContent();
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning($"Resume not found: {ex.Message}");
                return NotFound(new { error = ex.Message });
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning($"Unauthorized delete attempt: {ex.Message}");
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting resume {id}: {ex.Message}");
                return StatusCode(500, new { error = "An error occurred while deleting the resume" });
            }
        }
    }
}