using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Annotations;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class RegisterRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;

        [MaxLength(128)]
        [SwaggerSchema("The display name of the user.", ReadOnly = true)]
        public string? DisplayName { get; set; }
    }
}
