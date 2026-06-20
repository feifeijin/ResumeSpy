using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using ResumeSpy.UI.HealthChecks;
using Xunit;

namespace ResumeSpy.Tests.HealthChecks;

public class SupabaseAuthHealthCheckTests
{
    private static IConfiguration BuildConfig(string supabaseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = supabaseUrl
            })
            .Build();

    private static SupabaseAuthHealthCheck BuildCheck(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        Exception? exception = null,
        string supabaseUrl = "https://project.supabase.co")
    {
        var handler = new MockHttpMessageHandler(statusCode, exception);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SupabaseHealth"))
               .Returns(new HttpClient(handler) { BaseAddress = null });

        return new SupabaseAuthHealthCheck(factory.Object, BuildConfig(supabaseUrl));
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthy_WhenOidcEndpointReturns200()
    {
        var check = BuildCheck(HttpStatusCode.OK);
        var ctx = new HealthCheckContext { Registration = new HealthCheckRegistration("test", check, null, null) };

        var result = await check.CheckHealthAsync(ctx);

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsDegraded_WhenOidcEndpointReturnsNon200()
    {
        var check = BuildCheck(HttpStatusCode.NotFound);
        var ctx = new HealthCheckContext { Registration = new HealthCheckRegistration("test", check, null, null) };

        var result = await check.CheckHealthAsync(ctx);

        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("404", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthy_WhenOidcEndpointIsUnreachable()
    {
        var check = BuildCheck(exception: new HttpRequestException("connection refused"));
        var ctx = new HealthCheckContext { Registration = new HealthCheckRegistration("test", check, null, null) };

        var result = await check.CheckHealthAsync(ctx);

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_BuildsCorrectOidcUrl_FromSupabaseUrl()
    {
        string? capturedUrl = null;
        var handler = new CapturingHttpMessageHandler(u => capturedUrl = u);
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("SupabaseHealth"))
               .Returns(new HttpClient(handler));

        var check = new SupabaseAuthHealthCheck(factory.Object, BuildConfig("https://abc.supabase.co/"));
        var ctx = new HealthCheckContext { Registration = new HealthCheckRegistration("test", check, null, null) };

        await check.CheckHealthAsync(ctx);

        // Trailing slash on the input URL must be trimmed before /auth/v1 is appended
        Assert.Equal("https://abc.supabase.co/auth/v1/.well-known/openid-configuration", capturedUrl);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly Exception? _exception;

        public MockHttpMessageHandler(HttpStatusCode status = HttpStatusCode.OK, Exception? exception = null)
        {
            _status = status;
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_exception is not null) throw _exception;
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }

    private sealed class CapturingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Action<string> _capture;

        public CapturingHttpMessageHandler(Action<string> capture) => _capture = capture;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _capture(request.RequestUri?.ToString() ?? string.Empty);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
