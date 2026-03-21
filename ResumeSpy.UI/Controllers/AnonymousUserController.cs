using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Configuration;
using ResumeSpy.UI.Middlewares;
using ResumeSpy.UI.Models;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/anonymous-user")]
    public class AnonymousUserController : ControllerBase
    {
        private readonly IAnonymousUserService _anonymousUserService;
        private readonly ILogger<AnonymousUserController> _logger;
        private readonly AnonymousUserSettings _settings;

        public AnonymousUserController(
            IAnonymousUserService anonymousUserService,
            ILogger<AnonymousUserController> logger,
            IOptions<AnonymousUserSettings> settings)
        {
            _anonymousUserService = anonymousUserService;
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// Checks resume quota for the current anonymous user
        /// </summary>
        [HttpGet("check-quota")]
        public async Task<IActionResult> CheckResumeQuota()
        {
            try
            {
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                if (!anonymousUserId.HasValue)
                {
                    return Ok(new CheckResumeQuotaResponse
                    {
                        CurrentCount = 0,
                        MaxAllowed = _settings.MaxResumePerUser,
                        CanCreateResume = true
                    });
                }

                var count = await _anonymousUserService.GetResumeCountAsync(anonymousUserId.Value);
                var hasReachedLimit = await _anonymousUserService.HasReachedResumeLimitAsync(anonymousUserId.Value);

                return Ok(new CheckResumeQuotaResponse
                {
                    CurrentCount = count,
                    MaxAllowed = _settings.MaxResumePerUser,
                    CanCreateResume = !hasReachedLimit
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking resume quota");
                return StatusCode(500, new { error = "Failed to check resume quota" });
            }
        }

        /// <summary>
        /// Gets current anonymous user info
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetInfo()
        {
            try
            {
                var anonymousUserId = HttpContext.GetAnonymousUserId();
                if (!anonymousUserId.HasValue)
                {
                    return NotFound(new { error = "Anonymous user identity not found" });
                }

                var user = await _anonymousUserService.GetAsync(anonymousUserId.Value);
                if (user == null)
                {
                    return NotFound(new { error = "Anonymous user not found" });
                }

                return Ok(new
                {
                    anonymousUserId = user.Id,
                    resumeCount = user.ResumeCount,
                    isConverted = user.IsConverted
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting anonymous user info");
                return StatusCode(500, new { error = "Failed to get anonymous user info" });
            }
        }
    }
}
