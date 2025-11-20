using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Core.Entities.Business.Auth
{
    public class TokenRefreshResult
    {
        public bool Succeeded { get; set; }
        public TokenResult? Tokens { get; set; }
        public ApplicationUser? User { get; set; }
        public string[] Errors { get; set; } = System.Array.Empty<string>();

        public static TokenRefreshResult Failed(params string[] errors) => new()
        {
            Succeeded = false,
            Errors = errors
        };
    }
}
