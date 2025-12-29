using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using ResumeSpy.Core.Entities.Business;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.UI.Models;
using System.Security.Claims;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeDetailController : ControllerBase
    {

        private readonly ILogger<ResumeDetailController> _logger;
        private readonly IResumeDetailService _resumeDetailService;
        private readonly IGuestSessionService _guestSessionService;
        private readonly IMemoryCache _memoryCache;
        private readonly ITranslationService _translationService;
        private readonly IResumeManagementService _resumeManagementService;

        public ResumeDetailController(
            ILogger<ResumeDetailController> logger,
            IResumeDetailService resumeDetailService,
            IGuestSessionService guestSessionService,
            IMemoryCache memoryCache,
            ITranslationService translationService,
            IResumeManagementService resumeManagementService)
        {
            _logger = logger;
            _resumeDetailService = resumeDetailService;
            _guestSessionService = guestSessionService;
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
                var userId = User?.FindFirstValue(ClaimTypes.NameIdentifier);
                var guestSessionId = HttpContext.GetGuestSessionId();
                var guestIp = HttpContext.GetGuestIpAddress();

                // Handle first-time resume creation: validate guest limits
                var validationResponse = await ValidateFirstTimeResumeCreationAsync(resumeDetailModel, userId, guestSessionId);
                if (validationResponse != null)
                {
                    return validationResponse;
                }

                resumeDetailModel.CreateTime = DateTime.UtcNow.ToShortDateString();
                resumeDetailModel.LastModifyTime = DateTime.UtcNow.ToShortDateString();

                // ResumeManagementService handles resume creation and guest count increment atomically
                var result = await _resumeManagementService.CreateResumeDetailAsync(resumeDetailModel, userId, guestSessionId, guestIp);
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
            await _resumeManagementService.UpdateResumeDetailModelContentAsync(existingDetail);

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
            await _resumeManagementService.UpdateResumeDetailModelContentAsync(existingDetail);

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

        [HttpPost("{id}/sync-translations")]
        public async Task<IActionResult> SyncResumeDetailTranslationsAsync(string id)
        {
            try
            {
                var currentResumeDetail = await _resumeDetailService.GetResumeDetail(id);
                var allDetails = await _resumeDetailService.GetResumeDetailsByResumeId(currentResumeDetail.ResumeId);
                if (currentResumeDetail == null)
                {
                    return NotFound();
                }
                else if (allDetails == null || allDetails.Count() == 0)
                {
                    return NotFound();
                }
                else
                {
                    foreach (var detail in allDetails)
                    {
                        if (!string.IsNullOrWhiteSpace(detail.Language) && detail.Id != currentResumeDetail.Id)
                        {
                            string translatedContent = await _translationService.TranslateTextAsync(currentResumeDetail.Content, currentResumeDetail.Language ?? "", detail.Language);
                            detail.Content = translatedContent;
                            detail.LastModifyTime = DateTime.UtcNow.ToShortDateString();
                            await _resumeDetailService.Update(detail);
                        }
                    }
                }

                return Ok(new { message = "Successfully synchronized translations for resume details" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while synchronizing resume detail translations");
                return StatusCode(500, "An error occurred while synchronizing resume detail translations");
            }
        }

        private bool IsFirstTimeResumeCreation(ResumeDetailViewModel model)
        {
            return string.IsNullOrEmpty(model.ResumeId) || model.ResumeId == "undefined";
        }

        private async Task<ActionResult?> ValidateFirstTimeResumeCreationAsync(ResumeDetailViewModel model, string? userId, Guid? guestSessionId)
        {
            if (!IsFirstTimeResumeCreation(model))
            {
                return null;
            }

            // Authenticated user - no further validation needed
            if (!string.IsNullOrEmpty(userId))
            {
                _logger.LogInformation("User {UserId} creating resume", userId);
                return null;
            }

            // Guest or no session
            return await ValidateGuestResumeQuotaAsync(guestSessionId);
        }

        private async Task<ActionResult?> ValidateGuestResumeQuotaAsync(Guid? guestSessionId)
        {
            // Guest session not found - should not happen due to middleware
            if (!guestSessionId.HasValue)
            {
                return Unauthorized(new { error = "Guest session not found." });
            }

            // Check if guest has reached resume limit
            var hasReachedLimit = await _guestSessionService.HasReachedResumeLimitAsync(guestSessionId.Value);
            if (hasReachedLimit)
            {
                return StatusCode(403, new { error = "Guest resume limit reached. Please register to create more resumes." });
            }
            return null;
        }
    }

    public class CopyRequest
    {
        public required string ExistingResumeDetailId { get; set; }
        public required string Language { get; set; }
    }
}