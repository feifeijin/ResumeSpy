using System;

namespace ResumeSpy.Core.Exceptions
{
    public class QuotaExceededException : Exception
    {
        public QuotaExceededException()
        {
        }

        public QuotaExceededException(string? message) : base(message)
        {
        }

        public QuotaExceededException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
