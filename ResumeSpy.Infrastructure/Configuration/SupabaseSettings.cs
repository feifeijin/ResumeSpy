namespace ResumeSpy.Infrastructure.Configuration
{
    public class SupabaseSettings
    {
        public string Url { get; set; } = string.Empty;
        public string ServiceRoleKey { get; set; } = string.Empty;
        public string StorageBucket { get; set; } = string.Empty;
        public string JwtSecret { get; set; } = string.Empty;
    }
}
