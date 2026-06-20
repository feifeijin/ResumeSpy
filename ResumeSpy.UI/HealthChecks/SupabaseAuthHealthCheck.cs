using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ResumeSpy.UI.HealthChecks
{
    /// <summary>
    /// Checks that our backend can reach Supabase's OIDC discovery endpoint.
    /// A failure here means JWT validation will fail for every authenticated request.
    /// Registered at /health/auth so ops can distinguish a Supabase connectivity
    /// problem from an application bug.
    /// </summary>
    public class SupabaseAuthHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _oidcUrl;

        public SupabaseAuthHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            var supabaseUrl = (configuration["Supabase:Url"] ?? string.Empty).TrimEnd('/');
            _oidcUrl = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("SupabaseHealth");
                var response = await client.GetAsync(_oidcUrl, cancellationToken);
                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Degraded(
                        $"Supabase OIDC endpoint returned HTTP {(int)response.StatusCode}. " +
                        "JWT validation may fail.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(
                    $"Cannot reach Supabase OIDC endpoint ({_oidcUrl}). " +
                    "All authenticated requests will fail.",
                    ex);
            }
        }
    }
}
