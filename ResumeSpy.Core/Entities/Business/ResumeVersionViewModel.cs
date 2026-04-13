namespace ResumeSpy.Core.Entities.Business
{
    public class ResumeVersionViewModel
    {
        public Guid Id { get; set; }
        public string ResumeDetailId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Preview { get; set; } = string.Empty;
        public string? Label { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
