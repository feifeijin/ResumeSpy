namespace ResumeSpy.UI.Middlewares
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using System.Threading.Tasks;

    // Adds a baseline set of response headers that defend against MIME sniffing,
    // clickjacking, and referrer leakage. The API is JSON-only in production
    // (Swagger is dev-only), so the CSP locks the browser down to "no resources
    // at all" — there is no HTML to render and nothing legitimate to load.
    public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;

        public SecurityHeadersMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;

                headers["X-Content-Type-Options"] = "nosniff";
                headers["X-Frame-Options"] = "DENY";
                headers["Referrer-Policy"] = "no-referrer";

                // Swagger UI is dev-only and ships inline scripts/styles that a
                // strict CSP would block. Everywhere else the API returns JSON,
                // so the browser has no legitimate reason to load any resource.
                if (!context.Request.Path.StartsWithSegments("/swagger"))
                {
                    headers["Content-Security-Policy"] =
                        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";
                }

                return Task.CompletedTask;
            });

            return _next(context);
        }
    }

    public static class SecurityHeadersMiddlewareExtensions
    {
        public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
