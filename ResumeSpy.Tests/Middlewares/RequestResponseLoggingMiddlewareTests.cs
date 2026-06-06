using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ResumeSpy.UI.Middlewares;
using Xunit;

namespace ResumeSpy.Tests.Middlewares;

public class RequestResponseLoggingMiddlewareTests
{
    private sealed class CaptureLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }

    [Theory]
    [InlineData("Authorization", "Bearer eyJhbGciOiJIUzI1NiJ9.secret.sig")]
    [InlineData("Cookie", "session=abc123; token=xyz")]
    [InlineData("Set-Cookie", "session=abc123; HttpOnly")]
    [InlineData("X-API-Key", "sk-secret-key-12345")]
    [InlineData("X-Auth-Token", "very-secret-token")]
    public async Task Invoke_RedactsSensitiveRequestHeaders(string headerName, string sensitiveValue)
    {
        var logger = new CaptureLogger<RequestResponseLoggingMiddleware>();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/test";
        context.Request.Headers[headerName] = sensitiveValue;

        await middleware.Invoke(context);

        var allLogs = string.Join("\n", logger.Messages);
        Assert.DoesNotContain(sensitiveValue, allLogs, StringComparison.Ordinal);
        Assert.Contains("[REDACTED]", allLogs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_LogsNonSensitiveHeaders()
    {
        var logger = new CaptureLogger<RequestResponseLoggingMiddleware>();
        var middleware = new RequestResponseLoggingMiddleware(_ => Task.CompletedTask, logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Path = "/api/resume";
        context.Request.Headers["Content-Type"] = "application/json";

        await middleware.Invoke(context);

        var allLogs = string.Join("\n", logger.Messages);
        Assert.Contains("application/json", allLogs, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invoke_LogsMethodPathAndStatusCode()
    {
        var logger = new CaptureLogger<RequestResponseLoggingMiddleware>();
        var middleware = new RequestResponseLoggingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = 200;
            return Task.CompletedTask;
        }, logger);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/resume";

        await middleware.Invoke(context);

        var allLogs = string.Join("\n", logger.Messages);
        Assert.Contains("GET", allLogs, StringComparison.Ordinal);
        Assert.Contains("/api/resume", allLogs, StringComparison.Ordinal);
        Assert.Contains("200", allLogs, StringComparison.Ordinal);
    }
}
