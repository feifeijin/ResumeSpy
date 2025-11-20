using System.ComponentModel.DataAnnotations;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class ExternalAuthRequest
    {
        [Required]
        [MaxLength(32)]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// OAuth access token returned by the provider (e.g. GitHub access token).
        /// </summary>
        public string? AccessToken { get; set; }

        /// <summary>
        /// ID token returned by OpenID Connect providers (e.g. Google ID token).
        /// </summary>
        public string? IdToken { get; set; }

        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
