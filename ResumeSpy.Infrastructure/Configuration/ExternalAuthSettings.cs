namespace ResumeSpy.Infrastructure.Configuration
{
    public class ExternalAuthSettings
    {
        public GoogleAuthSettings Google { get; set; } = new();
        public GithubAuthSettings Github { get; set; } = new();
    }

    public class GoogleAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
    }

    public class GithubAuthSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string UserEndpoint { get; set; } = "https://api.github.com/user";
        public string EmailsEndpoint { get; set; } = "https://api.github.com/user/emails";
    }
}
