using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Entities.General;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PromptController : ControllerBase
    {
        private readonly IPromptProviderService _promptProvider;
        private readonly ILogger<PromptController> _logger;

        public PromptController(IPromptProviderService promptProvider, ILogger<PromptController> logger)
        {
            _promptProvider = promptProvider;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PromptTemplate>>> GetAll()
        {
            var prompts = await _promptProvider.GetAllAsync();
            return Ok(prompts);
        }

        [HttpPut("{key}")]
        public async Task<ActionResult> Upsert(string key, [FromBody] UpsertPromptRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SystemMessage))
                return BadRequest("SystemMessage is required.");

            var template = new PromptTemplate
            {
                Key = key,
                Category = request.Category ?? key,
                SystemMessage = request.SystemMessage,
                Description = request.Description,
                IsActive = true
            };

            await _promptProvider.UpsertAsync(template);
            _logger.LogInformation("Prompt template upserted for key: {Key}", key);
            return NoContent();
        }

        [HttpDelete("{key}")]
        public async Task<ActionResult> Delete(string key)
        {
            await _promptProvider.DeleteAsync(key);
            _logger.LogInformation("Prompt template deleted for key: {Key}, reverted to static default", key);
            return NoContent();
        }
    }

    public record UpsertPromptRequest(string SystemMessage, string? Category, string? Description);
}
