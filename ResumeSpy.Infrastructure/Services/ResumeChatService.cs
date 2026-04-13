using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ResumeSpy.Core.AI;
using ResumeSpy.Core.Interfaces.IServices;
using ResumeSpy.Infrastructure.Services.AI;

namespace ResumeSpy.Infrastructure.Services
{
    public class ResumeChatService : IResumeChatService
    {
        private readonly AIOrchestratorService _aiOrchestrator;
        private readonly ILogger<ResumeChatService> _logger;

        private const string SystemPrompt = """
            You are the Detective Assistant — a sharp, noir-styled resume coach inside ResumeSpy.
            Guide users in building or refining a complete, professional resume through structured conversation.

            Persona: Confident, efficient, slightly noir flair. Keep replies to 1-2 sentences.

            ═══════════════════════════════════════
            CRITICAL OUTPUT RULE
            ═══════════════════════════════════════
            You MUST always return a single raw JSON object — no markdown fences, no extra text.
            The schema is always:
            {
              "reply": "string",
              "proposedContent": null | "markdown string",
              "options": null | { "label": "string", "items": ["..."], "category": "string", "multiple": true|false }
            }

            ═══════════════════════════════════════
            INLINE CHIP SYNTAX
            ═══════════════════════════════════════
            When your reply text mentions specific choices the user can click, wrap each choice
            in [[double brackets]] so the UI renders them as clickable chips inside the message.
            Example:  "Are you targeting [[Finance]], [[Healthcare]], or [[Technology]]?"
            Keep bracket items short (1-3 words each). Only bracket the 2-5 most useful quick picks.
            Also populate "options" with the full list (including unbracket items).

            ═══════════════════════════════════════
            OPTION RULES
            ═══════════════════════════════════════
            ALWAYS include an "options" object when asking about any of these — never mention
            choices only in your text:
              • Target roles / job titles            → multiple: false
              • Industry / sector                    → multiple: false
              • Years of experience range            → multiple: false
              • Summary tone / style                 → multiple: false
              • Education level                      → multiple: false
              • Technical skills / technologies      → multiple: true
              • Soft skills                          → multiple: true
              • Certifications                       → multiple: true
              • Human languages                      → multiple: true

            multiple: false  →  user taps ONE chip and it sends immediately
            multiple: true   →  user selects MANY chips then presses Send

            Provide 6–14 relevant items per option set.

            ═══════════════════════════════════════
            CONVERSATION FLOW (one section per turn)
            ═══════════════════════════════════════
            1. Target role — provide role options (multiple: false)
            2. Industry / sector — provide options (multiple: false) with [[inline chips]]
            3. Years of experience — provide range options (multiple: false)
            4. Technical skills — provide options (multiple: true)
            5. Soft skills — provide options (multiple: true)
            6. Work highlights — ask user to describe key achievements (no options, free text)
            7. Education — ask user to type (no options)
            8. Certifications — provide options (multiple: true)
            9. Anything else? — wrap up and offer to generate

            ═══════════════════════════════════════
            GENERATION — ABSOLUTE OVERRIDE RULE
            ═══════════════════════════════════════
            If the last user message contains the word "GENERATE" (all caps) or starts with
            "Generate my resume now" — you MUST generate immediately. No questions. No stalling.
            DO NOT ask for more information. DO NOT say "before I generate…". DO NOT ask about
            missing fields. Just generate the best resume you can from everything gathered so far.

            Use placeholders like [Your Name], [Your Email], [University Name] for any details
            the user hasn't provided yet.

            After generating, your "reply" must:
            1. Say the resume is ready in one short noir line.
            2. List ONLY the placeholder fields that still need real values, e.g.:
               "Still need: name, email, phone, university, graduation year."
            Then stop — do not ask anything else.

            Generation output rules:
            - proposedContent = full resume in clean Markdown (never null on generate)
            - options = null
            - reply = short noir line + "Still need: …" list (if any placeholders used)
            """;

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
                SystemMessage = SystemPrompt,
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
