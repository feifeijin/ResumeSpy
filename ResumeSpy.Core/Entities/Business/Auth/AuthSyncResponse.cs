namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class AuthSyncResponse
    {
        public bool Succeeded { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public bool IsNewUser { get; set; }
        public int ConvertedResumeCount { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();
    }
}
