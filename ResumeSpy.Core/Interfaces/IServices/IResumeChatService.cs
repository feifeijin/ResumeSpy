namespace ResumeSpy.Core.Interfaces.IServices
{
    public record ChatMessage(string Role, string Content);

    public record OptionSet(string Label, string[] Items, string Category, bool Multiple = false);

    public record ChatResponse(string Reply, string? ProposedContent, OptionSet? Options = null);

    public interface IResumeChatService
    {
        Task<ChatResponse> ChatAsync(
            IReadOnlyList<ChatMessage> history,
            string currentResumeContent,
            string? language = null);
    }
}
