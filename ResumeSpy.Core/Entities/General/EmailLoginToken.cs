using System;

namespace ResumeSpy.Core.Entities.General
{
    public class EmailLoginToken : Base<int>
    {
        public string UserId { get; set; } = string.Empty;
        public string TokenHash { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? ConsumedAtUtc { get; set; }
        public string? RedirectUrl { get; set; }

        public ApplicationUser User { get; set; } = default!;
    }
}
