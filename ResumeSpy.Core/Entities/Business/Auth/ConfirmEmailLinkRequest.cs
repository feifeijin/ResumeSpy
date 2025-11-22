using System.ComponentModel.DataAnnotations;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class ConfirmEmailLinkRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;
    }
}
