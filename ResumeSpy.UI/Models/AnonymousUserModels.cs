namespace ResumeSpy.UI.Models
{
    public class CheckResumeQuotaResponse
    {
        public int CurrentCount { get; set; }
        public int MaxAllowed { get; set; }
        public bool CanCreateResume { get; set; }
    }
}
