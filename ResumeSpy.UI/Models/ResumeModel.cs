namespace ResumeSpy.UI.Models
{
    public class ResumeModel
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int ResumeDetailCount { get; set; }
        public string ResumeImgPath { get; set; } = string.Empty;
        public DateTime CreateTime { get; set; }
        public DateTime LastModifyTime { get; set; }

    }
}