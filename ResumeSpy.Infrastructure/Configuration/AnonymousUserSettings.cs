namespace ResumeSpy.Infrastructure.Configuration
{
    public class AnonymousUserSettings
    {
        /// <summary>
        /// Maximum number of resumes an anonymous user can create.
        /// Default: 1
        /// </summary>
        public int MaxResumePerUser { get; set; } = 1;

        /// <summary>
        /// Maximum number of AI-backed calls (import, chat, tailor) per identity
        /// per UTC day. Applies to both authenticated users and anonymous users —
        /// the cap protects the upstream HuggingFace / OpenAI quota from
        /// cost-DoS abuse. Set &lt;= 0 to disable.
        /// </summary>
        public int MaxAiCallsPerDay { get; set; } = 30;
    }
}
