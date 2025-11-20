using System;
using System.Collections.Generic;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class AuthResponse
    {
        public bool Succeeded { get; set; }
        public string? AccessToken { get; set; }
        public DateTime? AccessTokenExpiresAt { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public bool IsNewUser { get; set; }
        public IEnumerable<string> Errors { get; set; } = Array.Empty<string>();

        public static AuthResponse Failed(params string[] errors) => new()
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
