namespace ResumeSpy.Infrastructure.Configuration
{
    public class AnonymousUserSettings
    {
        /// <summary>
        /// Maximum number of resumes an anonymous user can create.
        /// Default: 1
        /// </summary>
        public int MaxResumePerUser { get; set; } = 1;
    }
}
