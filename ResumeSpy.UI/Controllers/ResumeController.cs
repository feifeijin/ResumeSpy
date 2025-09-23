using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;
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
        private readonly IMemoryCache _memoryCache;

        public ResumeController(
            ILogger<ResumeController> logger, 
            IResumeService resumeService, 
            IResumeManagementService resumeManagementService,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _resumeService = resumeService;
            _resumeManagementService = resumeManagementService;
            _memoryCache = memoryCache;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeViewModel>>> GetResumes()
        {
            int page = 1;
            int pageSize = 100;

            var resumes = await _resumeService.GetPaginatedResumes(page, pageSize);
            var pagedResumes = new StaticPagedList<ResumeViewModel>(resumes.Data, page, pageSize, resumes.TotalCount);

            return Ok(pagedResumes);
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
                var createdResume = await _resumeService.Create(resume);
                return CreatedAtAction(nameof(GetResume), new { id = createdResume.Id }, createdResume);
            }
            catch (Exception ex)
            {
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