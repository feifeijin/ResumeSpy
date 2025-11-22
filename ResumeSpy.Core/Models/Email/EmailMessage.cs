using System.Collections.Generic;

namespace ResumeSpy.Core.Models.Email
{
    public class EmailMessage
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? HtmlBody { get; set; }
        public string? TextBody { get; set; }
        public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}
