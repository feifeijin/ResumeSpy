using System;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class TokenResult
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime AccessTokenExpiresAt { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime RefreshTokenExpiresAt { get; set; }
        public string RefreshTokenId { get; set; } = string.Empty;
    }
}
