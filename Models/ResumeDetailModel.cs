namespace ResumeSpy.Models
{
    public class ResumeDetailModel
    {
        public string Id { get; set; }
        public string ResumeId { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string Content { get; set; }
        public bool IsDefault { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime LastModifyTime { get; set; }
    }
}