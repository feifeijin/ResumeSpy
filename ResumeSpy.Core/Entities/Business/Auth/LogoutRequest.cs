using System.ComponentModel.DataAnnotations;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class LogoutRequest
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
