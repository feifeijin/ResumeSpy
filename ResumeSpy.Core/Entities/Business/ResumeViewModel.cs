using Swashbuckle.AspNetCore.Annotations;

namespace ResumeSpy.Core.Entities.Business
{
    public class ResumeViewModel
    {
        public required string Id { get; set; }
        
        public string? Title { get; set; }
        
        public int ResumeDetailCount { get; set; }
        public string? ResumeImgPath { get; set; }
        public string? EntryDate { get; set; }
        public string? UpdateDate { get; set; }
    }
}