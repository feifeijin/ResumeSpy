using System;

namespace ResumeSpy.Core.Exceptions
{
    /// <summary>
    /// Thrown when the AI provider chain has exhausted retries / failed open
    /// circuit breakers / hit the request timeout. Controllers map this to a
    /// 503 with a "try again later" message instead of a 500.
    /// </summary>
    public class AiServiceUnavailableException : Exception
    {
        public AiServiceUnavailableException()
        {
        }

        public AiServiceUnavailableException(string? message) : base(message)
        {
        }

        public AiServiceUnavailableException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
