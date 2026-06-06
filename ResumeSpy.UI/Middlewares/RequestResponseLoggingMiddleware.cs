namespace ResumeSpy.UI.Middlewares
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System.Text;
    using System.Threading.Tasks;

    public class RequestResponseLoggingMiddleware
    {
        // Headers that carry credentials or session tokens and must never appear in logs.
        private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "Authorization",
            "Cookie",
            "Set-Cookie",
            "X-API-Key",
            "X-Auth-Token",
        };

        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            LogRequest(context.Request);
            await _next(context);
            LogResponse(context.Response);
        }

        private void LogRequest(HttpRequest request)
        {
            _logger.LogInformation("Request: {Method} {Path}", request.Method, request.Path);
            _logger.LogDebug("Request headers: {Headers}", GetHeadersAsString(request.Headers));
        }

        private void LogResponse(HttpResponse response)
        {
            _logger.LogInformation("Response: {StatusCode}", response.StatusCode);
            _logger.LogDebug("Response headers: {Headers}", GetHeadersAsString(response.Headers));
        }

        private static string GetHeadersAsString(IHeaderDictionary headers)
        {
            var sb = new StringBuilder();
            foreach (var (key, value) in headers)
            {
                var displayValue = SensitiveHeaders.Contains(key) ? "[REDACTED]" : value.ToString();
                sb.AppendLine($"{key}: {displayValue}");
            }
            return sb.ToString();
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class RequestResponseLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
}
