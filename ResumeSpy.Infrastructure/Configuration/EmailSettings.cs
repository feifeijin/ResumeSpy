using System;

namespace ResumeSpy.Infrastructure.Configuration
{
    public class EmailSettings
    {
        public EmailFromSettings From { get; set; } = new();
        public SmtpSettings Smtp { get; set; } = new();
        public MagicLinkSettings MagicLink { get; set; } = new();
        public EmailTemplateSettings Templates { get; set; } = new();
    }

    public class EmailFromSettings
    {
        public string Address { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class SmtpSettings
    {
        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 1025;
        public bool UseSsl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class MagicLinkSettings
    {
        public string ClientCallbackUrl { get; set; } = "http://localhost:5173/auth/magic";
        public int ExpiryMinutes { get; set; } = 15;
    }

    public class EmailTemplateSettings
    {
        public EmailTemplate MagicLink { get; set; } = new();
    }

    public class EmailTemplate
    {
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string TextBody { get; set; } = string.Empty;

        public string RenderHtml(Func<string, string> replacer)
        {
            return replacer(HtmlBody);
        }

        public string RenderText(Func<string, string> replacer)
        {
            return replacer(TextBody);
        }
    }
}
