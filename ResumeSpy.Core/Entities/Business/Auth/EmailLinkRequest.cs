using System.ComponentModel.DataAnnotations;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class EmailLinkRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? RedirectUrl { get; set; }
    }
}
