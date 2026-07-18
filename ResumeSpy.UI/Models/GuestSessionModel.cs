namespace ResumeSpy.UI.Models
{
    public class CreateGuestSessionRequest
    {
        public string? UserAgent { get; set; }
    }

    public class CreateGuestSessionResponse
    {
        public Guid SessionId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

    public class CheckResumeQuotaResponse
    {
        public int CurrentCount { get; set; }
        public int MaxAllowed { get; set; }
        public bool CanCreateResume { get; set; }
    }
}
