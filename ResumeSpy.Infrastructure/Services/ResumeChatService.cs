using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Prompts;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services
{
    public class ResumeChatService : IResumeChatService
    {
        private readonly AIOrchestratorService _aiOrchestrator;
        private readonly ILogger<ResumeChatService> _logger;

        public ResumeChatService(AIOrchestratorService aiOrchestrator, ILogger<ResumeChatService> logger)
        {
            _aiOrchestrator = aiOrchestrator;
            _logger = logger;
        }

        public async Task<ChatResponse> ChatAsync(
            IReadOnlyList<ChatMessage> history,
            string currentResumeContent,
            string? language = null)
        {
            // Build the full prompt from history + current resume
            var prompt = BuildPrompt(history, currentResumeContent, language);

            var request = new AIRequest
            {
                Prompt = prompt,
                SystemMessage = ChatPrompts.SystemPrompt,
                Temperature = 0.7,
                MaxTokens = 4096
            };

            var response = await _aiOrchestrator.ExecuteTextGenerationAsync(request, useCache: false);

            if (!response.IsSuccess || string.IsNullOrWhiteSpace(response.Content))
            {
                _logger.LogError("Chat AI call failed: {Error}", response.ErrorMessage);
                throw new InvalidOperationException($"Chat failed: {response.ErrorMessage}");
            }

            return ParseResponse(response.Content);
        }

        private static string BuildPrompt(
            IReadOnlyList<ChatMessage> history,
            string currentResumeContent,
            string? language)
        {
            var sb = new StringBuilder();

            sb.AppendLine("## Current Resume (Markdown):");
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(currentResumeContent)
                ? "(No resume content yet — help the user build one from scratch.)"
                : currentResumeContent);
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(language))
            {
                sb.AppendLine($"Resume language: {language}");
                sb.AppendLine();
            }

            sb.AppendLine("## Conversation History:");
            sb.AppendLine();

            foreach (var msg in history)
            {
                var role = msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase) ? "User" : "Detective";
                sb.AppendLine($"{role}: {msg.Content}");
                sb.AppendLine();
            }

            sb.AppendLine("## Your response (JSON only):");

            return sb.ToString();
        }

        private static ChatResponse ParseResponse(string raw)
        {
            // Strip markdown code fences if the model wrapped the JSON anyway
            var cleaned = raw.Trim();
            if (cleaned.StartsWith("```"))
            {
                var start = cleaned.IndexOf('{');
                var end = cleaned.LastIndexOf('}');
                if (start >= 0 && end > start)
                    cleaned = cleaned[start..(end + 1)];
            }

            try
            {
                var doc = JsonDocument.Parse(cleaned);
                var root = doc.RootElement;

                var reply = root.GetProperty("reply").GetString() ?? "Case under investigation…";

                string? proposedContent = null;
                if (root.TryGetProperty("proposedContent", out var pc) && pc.ValueKind == JsonValueKind.String)
                {
                    proposedContent = pc.GetString();
                    if (string.IsNullOrWhiteSpace(proposedContent)) proposedContent = null;
                }

                OptionSet? options = null;
                if (root.TryGetProperty("options", out var optEl) && optEl.ValueKind == JsonValueKind.Object)
                {
                    var label    = optEl.TryGetProperty("label",    out var lv) ? lv.GetString() ?? "" : "";
                    var category = optEl.TryGetProperty("category", out var cv) ? cv.GetString() ?? "" : "";
                    string[] items = [];
                    if (optEl.TryGetProperty("items", out var iv) && iv.ValueKind == JsonValueKind.Array)
                    {
                        items = iv.EnumerateArray()
                            .Select(x => x.GetString() ?? "")
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToArray();
                    }
                    bool multiple = optEl.TryGetProperty("multiple", out var mv) && mv.ValueKind == JsonValueKind.True;
                    if (!string.IsNullOrWhiteSpace(label) && items.Length > 0)
                        options = new OptionSet(label, items, category, multiple);
                }

                return new ChatResponse(reply, proposedContent, options);
            }
            catch
            {
                return new ChatResponse(cleaned, null, null);
            }
        }
    }
}
