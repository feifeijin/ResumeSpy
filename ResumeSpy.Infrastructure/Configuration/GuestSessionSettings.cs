namespace ResumeSpy.Infrastructure.Configuration
{
    /// <summary>
    /// Configuration settings for guest session management
    /// </summary>
    public class GuestSessionSettings
    {
        /// <summary>
        /// Number of days before a guest session expires
        /// Default: 30 days
        /// </summary>
        public int SessionExpiryDays { get; set; } = 30;

        /// <summary>
        /// Maximum number of resumes a guest can create per session
        /// Default: 1
        /// </summary>
        public int MaxResumePerSession { get; set; } = 1;

        /// <summary>
        /// Maximum number of guest sessions allowed from a single IP per day
        /// Prevents abuse via incognito mode
        /// Default: 5 sessions per day
        /// </summary>
        public int MaxSessionsPerIpPerDay { get; set; } = 5;

        /// <summary>
        /// Maximum number of resumes allowed from a single IP per day across all sessions
        /// Prevents quota bypass via multiple browsers/incognito
        /// Default: 3 resumes per day
        /// </summary>
        public int MaxResumesPerIpPerDay { get; set; } = 3;

        /// <summary>
        /// Whether to strictly validate IP addresses (reject if IP changes)
        /// When false, IP changes are logged but allowed (mobile-friendly)
        /// Default: false (recommended for Option 4)
        /// </summary>
        public bool StrictIpValidation { get; set; } = false;

        /// <summary>
        /// Whether to log IP address changes for security monitoring
        /// Default: true
        /// </summary>
        public bool LogIpChanges { get; set; } = true;

        /// <summary>
        /// Enable IP-based rate limiting to prevent abuse
        /// Default: true
        /// </summary>
        public bool EnableRateLimiting { get; set; } = true;
    }
}
