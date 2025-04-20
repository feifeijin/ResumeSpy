namespace ResumeSpy.Core.Entities.Business
{
    public class ResumeDetailViewModel
    {
        public required string Id { get; set; }
        public required string ResumeId { get; set; }
        public string? Name { get; set; }
        public required string Language { get; set; }
        public required string Content { get; set; }
        public bool IsDefault { get; set; }
        public  string? EntryDate { get; set; }
        public  string? UpdateDate { get; set; }

    }
}