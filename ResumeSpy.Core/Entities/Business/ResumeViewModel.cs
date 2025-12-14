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

        // Guest-related fields
        public string? UserId { get; set; }
        public Guid? GuestSessionId { get; set; }
        public bool IsGuest { get; set; } = false;
        public string? CreatedIpAddress { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}