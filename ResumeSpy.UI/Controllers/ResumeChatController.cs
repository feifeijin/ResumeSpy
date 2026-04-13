using Microsoft.AspNetCore.Mvc;
using ResumeSpy.Core.Interfaces.IServices;

namespace ResumeSpy.UI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResumeChatController : ControllerBase
    {
        private readonly ILogger<ResumeChatController> _logger;
        private readonly IResumeChatService _chatService;

        public ResumeChatController(ILogger<ResumeChatController> logger, IResumeChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        /// <summary>
        /// POST api/resumeChat/message
        /// Send a chat message. Returns the detective's reply and optionally a proposed resume update.
        /// </summary>
        [HttpPost("message")]
        public async Task<ActionResult<ChatMessageResponse>> SendMessage([FromBody] ChatMessageRequest request)
        {
            if (request.Messages == null || request.Messages.Count == 0)
                return BadRequest(new { error = "messages must contain at least one entry." });

            try
            {
                var history = request.Messages
                    .Select(m => new ChatMessage(m.Role, m.Content))
                    .ToList();

                var result = await _chatService.ChatAsync(history, request.CurrentContent, request.Language);

                return Ok(new ChatMessageResponse
                {
                    Reply = result.Reply,
                    ProposedContent = result.ProposedContent,
                    Options = result.Options == null ? null : new OptionSetDto
                    {
                        Label = result.Options.Label,
                        Items = result.Options.Items,
                        Category = result.Options.Category,
                        Multiple = result.Options.Multiple
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Chat request failed");
                return StatusCode(500, new { error = "The detective is momentarily unavailable. Try again." });
            }
        }
    }

    public class ChatMessageRequest
    {
        public required string CurrentContent { get; set; }
        public string? Language { get; set; }
        public required List<ChatMessageDto> Messages { get; set; }
    }

    public class ChatMessageDto
    {
        public required string Role { get; set; }    // "user" | "assistant"
        public required string Content { get; set; }
    }

    public class ChatMessageResponse
    {
        public required string Reply { get; set; }
        public string? ProposedContent { get; set; }
        public OptionSetDto? Options { get; set; }
    }

    public class OptionSetDto
    {
        public required string Label { get; set; }
        public required string[] Items { get; set; }
        public required string Category { get; set; }
        public bool Multiple { get; set; }
    }
}
