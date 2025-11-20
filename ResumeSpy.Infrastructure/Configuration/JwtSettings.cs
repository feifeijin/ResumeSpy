namespace ResumeSpy.Infrastructure.Configuration
{
    public class JwtSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string SigningKey { get; set; } = string.Empty;
        public int AccessTokenDurationInMinutes { get; set; } = 60;
        public int RefreshTokenDurationInDays { get; set; } = 7;
    }
}
