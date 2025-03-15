namespace ResumeSpy.Models
{
    public class ResumeModel
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public int ResumeDetailCount { get; set; }
        public string ResumeImgPath { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastModifyTime { get; set; }

    }
}