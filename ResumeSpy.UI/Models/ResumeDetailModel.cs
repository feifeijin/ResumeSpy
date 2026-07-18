namespace ResumeSpy.UI.Models
{
    public class ResumeDetailModel
    {
        public string Id { get; set; } = string.Empty;
        public string ResumeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastModifyTime { get; set; }
    }
}