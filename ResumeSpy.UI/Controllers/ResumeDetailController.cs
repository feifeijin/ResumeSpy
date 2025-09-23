using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Models;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeDetailController : ControllerBase
    {

        private readonly ILogger<ResumeController> _logger;
        private readonly IResumeDetailService _resumeDetailService;
        private readonly IMemoryCache _memoryCache;
        private readonly ITranslationService _translationService;
        private readonly IResumeManagementService _resumeManagementService;

        public ResumeDetailController(
            ILogger<ResumeController> logger,
            IResumeDetailService resumeDetailService,
            IMemoryCache memoryCache,
            ITranslationService translationService,
            IResumeManagementService resumeManagementService)
        {
            _logger = logger;
            _resumeDetailService = resumeDetailService;
            _memoryCache = memoryCache;
            _translationService = translationService;
            _resumeManagementService = resumeManagementService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ResumeDetailViewModel>>> GetResumeDetailModelsAsync([FromQuery] string resumeId)
        {
            var details = (await _resumeDetailService.GetResumeDetailsByResumeId(resumeId)).ToList();
            if (details.Count() == 0)
            {
                details.Add(new ResumeDetailViewModel
                {
                    Id = string.Empty,
                    ResumeId = resumeId,
                    Name = "Default",
                    Language = string.Empty,
                    Content = string.Empty,
                    IsDefault = true,
                    CreateTime = DateTime.UtcNow.ToShortDateString(),
                    LastModifyTime = DateTime.UtcNow.ToShortDateString()
                });
            }
            return Ok(details);
        }

        [HttpPost]
        public async Task<ActionResult<ResumeDetailViewModel>> CreateResumeDetailModelAsync([FromBody] ResumeDetailViewModel resumeDetailModel)
        {
            try
            {
                resumeDetailModel.CreateTime = DateTime.UtcNow.ToShortDateString();
                resumeDetailModel.LastModifyTime = DateTime.UtcNow.ToShortDateString();
                
                var result = await _resumeManagementService.CreateResumeDetailAsync(resumeDetailModel);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resume detail");
                return StatusCode(500, "An error occurred while creating the resume detail");
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<ResumeDetailViewModel>> UpdateResumeDetailModelAsync(string id, [FromBody] ResumeDetailViewModel updatedDetail)
        {
            var existingDetail = await _resumeDetailService.GetResumeDetail(updatedDetail.Id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Name = updatedDetail.Name;
            existingDetail.Language = updatedDetail.Language;
            existingDetail.Content = updatedDetail.Content;
            existingDetail.IsDefault = updatedDetail.IsDefault;
            existingDetail.LastModifyTime = DateTime.UtcNow.ToShortDateString();
            await _resumeDetailService.Update(existingDetail);

            return Ok(existingDetail);
        }

        [HttpPatch("{id}/name")]
        public async Task<ActionResult<ResumeDetailViewModel>> UpdateResumeDetailModelNameAsync(string id, [FromBody] string newName)
        {

            var existingDetail = await _resumeDetailService.GetResumeDetail(id);

            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Name = newName;
            existingDetail.LastModifyTime = DateTime.UtcNow.ToShortDateString();
            await _resumeDetailService.Update(existingDetail);
            return Ok(existingDetail);
        }

        [HttpPatch("{id}/content")]
        public async Task<ActionResult<ResumeDetailViewModel>> UpdateResumeDetailModelContentAsync(string id, [FromBody] string content)
        {
            var existingDetail = await _resumeDetailService.GetResumeDetail(id);

            if (existingDetail == null)
            {
                return NotFound();
            }

            existingDetail.Content = content;
            existingDetail.LastModifyTime = DateTime.UtcNow.ToShortDateString();
            await _resumeDetailService.Update(existingDetail);

            return Ok(existingDetail);
        }

        [HttpPost("copy")]
        public async Task<ActionResult<ResumeDetailViewModel>> CreateResumeDetailModelFromExisting([FromBody] CopyRequest request)
        {
            var existingDetail = await _resumeDetailService.GetResumeDetail(request.ExistingResumeDetailId);
            if (existingDetail == null)
            {
                return NotFound();
            }

            string translatedContent = await _translationService.TranslateTextAsync(existingDetail.Content, existingDetail.Language, request.Language);

            var newDetail = new ResumeDetailViewModel
            {
                Id = Guid.NewGuid().ToString(),
                ResumeId = existingDetail.ResumeId,
                Name = existingDetail.Name + " (Translated)",
                Language = request.Language,
                Content = translatedContent,
                IsDefault = false,
                CreateTime = DateTime.UtcNow.ToShortDateString(),
                LastModifyTime = DateTime.UtcNow.ToShortDateString()
            };
            var result = await _resumeDetailService.Create(newDetail);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResumeDetailModelAsync(string id)
        {
            var existingDetail = await _resumeDetailService.GetResumeDetail(id);
            if (existingDetail == null)
            {
                return NotFound();
            }

            await _resumeDetailService.Delete(existingDetail.Id);
            return NoContent();
        }

        [HttpPatch("{id}/set-default")]
        public async Task<IActionResult> SetDefaultResumeDetailAsync(string id)
        {
            try
            {
                await _resumeManagementService.SetDefaultResumeDetailAsync(id);
                return Ok(new { message = "Successfully set as default and updated resume image path" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while setting ResumeDetail {ResumeDetailId} as default", id);
                return StatusCode(500, "An error occurred while setting the resume detail as default");
            }
        }
    }

    public class CopyRequest
    {
        public required string ExistingResumeDetailId { get; set; }
        public required string Language { get; set; }
    }
}