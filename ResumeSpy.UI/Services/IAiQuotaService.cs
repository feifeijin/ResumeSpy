namespace ResumeSpy.UI.Services
{
    /// <summary>
    /// Per-identity daily quota for AI-backed endpoints (import, chat, tailor).
    /// Mirrors <c>AnonymousUserSettings.MaxResumePerUser</c> but for AI calls so a
    /// single user (or anonymous GUID) cannot exhaust the upstream HuggingFace /
    /// OpenAI quota for everyone else.
    /// </summary>
    public interface IAiQuotaService
    {
        /// <summary>
        /// Atomically increments the daily counter for <paramref name="identityKey"/>
        /// and returns whether the call is allowed. <paramref name="identityKey"/>
        /// must be unique per user / anonymous GUID (e.g. <c>"user:abc"</c> or
        /// <c>"anon:guid"</c>).
        /// </summary>
        AiQuotaResult TryConsume(string identityKey);
    }

    public readonly record struct AiQuotaResult(bool Allowed, int Remaining, int Max);
}
