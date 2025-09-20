using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Models;
using X.PagedList;


namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly ILogger<ResumeController> _logger;
        private readonly IResumeService _resumeService;
        private readonly IMemoryCache _memoryCache;

        public ResumeController(ILogger<ResumeController> logger, IResumeService resumeService, IMemoryCache memoryCache)
        {
            _logger = logger;
            _resumeService = resumeService;
            _memoryCache = memoryCache;
        }

        private static List<ResumeModel> Resumes = new List<ResumeModel>
        {
            // new ResumeModel
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 1",
            //     ResumeDetailCount = 3,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     CreateTime = DateTime.UtcNow,
            //     LastModifyTime = DateTime.UtcNow
            // },
            // new ResumeModel
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 2",
            //     ResumeDetailCount = 2,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     CreateTime = DateTime.UtcNow,
            //     LastModifyTime = DateTime.UtcNow

            // },
            // new ResumeModel
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 3",
            //     ResumeDetailCount = 5,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     CreateTime = DateTime.UtcNow,
            //     LastModifyTime = DateTime.UtcNow
            // },
            // new ResumeModel
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 4",
            //     ResumeDetailCount = 1,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     CreateTime = DateTime.UtcNow,
            //     LastModifyTime = DateTime.UtcNow
            // },
            // new ResumeModel
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 5",
            //     ResumeDetailCount = 4,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     CreateTime = DateTime.UtcNow,
            //     LastModifyTime = DateTime.UtcNow
            // }
        };

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ResumeModel>>> GetResumes()
        {
            int page = 1;
            int pageSize = 100;

            var resumes = await _resumeService.GetPaginatedResumes(page, pageSize);
            var pagedResumes = new StaticPagedList<ResumeViewModel>(resumes.Data, page, pageSize, resumes.TotalCount);

            return Ok(pagedResumes);
        }

        [HttpGet("{id}")]
        public ActionResult<ResumeModel> GetResume(string id)
        {
            var resume = Resumes.FirstOrDefault(r => r.Id == id);
            if (resume == null)
            {
                return NotFound();
            }
            return Ok(resume);
        }

        [HttpPost]
        public ActionResult<ResumeModel> CreateResume([FromBody] ResumeModel resume)
        {
            resume.Id = Guid.NewGuid().ToString();
            resume.CreateTime = DateTime.UtcNow;
            resume.LastModifyTime = DateTime.UtcNow;
            Resumes.Add(resume);
            return CreatedAtAction(nameof(GetResume), new { id = resume.Id }, resume);
        }

        [HttpPut("{id}")]
        public ActionResult<ResumeModel> UpdateResume(string id, [FromBody] ResumeModel updatedResume)
        {
            var existingResume = Resumes.FirstOrDefault(r => r.Id == id);
            if (existingResume == null)
            {
                return NotFound();
            }

            existingResume.Title = updatedResume.Title;
            existingResume.ResumeDetailCount = updatedResume.ResumeDetailCount;
            existingResume.ResumeImgPath = updatedResume.ResumeImgPath;
            existingResume.LastModifyTime = DateTime.UtcNow;

            return Ok(existingResume);
        }

        [HttpPatch("{id}/title")]
        public ActionResult<ResumeModel> UpdateResumeName(string id, [FromBody] string title)
        {
            var existingResume = Resumes.FirstOrDefault(r => r.Id == id);
            if (existingResume == null)
            {
                return NotFound();
            }

            existingResume.Title = title;
            existingResume.LastModifyTime = DateTime.UtcNow;

            return Ok(existingResume);
        }

        [HttpPost("{id}/clone")]
        public ActionResult<ResumeModel> CloneResume(string id)
        {
            var existingResume = Resumes.FirstOrDefault(r => r.Id == id);
            if (existingResume == null)
            {
                return NotFound();
            }

            var clonedResume = new ResumeModel
            {
                Id = Guid.NewGuid().ToString(),
                Title = existingResume.Title + " (Copy)",
                ResumeDetailCount = existingResume.ResumeDetailCount,
                ResumeImgPath = existingResume.ResumeImgPath,
                CreateTime = DateTime.UtcNow,
                LastModifyTime = DateTime.UtcNow,
            };

            Resumes.Add(clonedResume);
            return CreatedAtAction(nameof(GetResume), new { id = clonedResume.Id }, clonedResume);
        }

        [HttpDelete("{id}")]
        public ActionResult DeleteResume(string id)
        {
            var existingResume = Resumes.FirstOrDefault(r => r.Id == id);
            if (existingResume == null)
            {
                return NotFound();
            }

            Resumes.Remove(existingResume);
            return NoContent();
        }
    }
}