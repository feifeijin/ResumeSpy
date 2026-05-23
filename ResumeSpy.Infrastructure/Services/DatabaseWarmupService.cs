using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ResumeSpy.Infrastructure.Data;

namespace ResumeSpy.Infrastructure.Services
{
    /// <summary>
    /// Fires a trivial SQL query at startup to prime the Npgsql connection pool.
    ///
    /// Without this, every cold start (new deployment, container restart, or idle-period
    /// connection-pool eviction) forces the very first user request to pay the full
    /// TCP-connect + TLS-handshake + Postgres-authentication cost, which typically
    /// adds 1–3 s of latency on the resume list load.
    ///
    /// By opening at least one connection during <see cref="StartAsync"/> the pool is
    /// ready before traffic arrives, so users experience normal, sub-100 ms response
    /// times even on the first page load after a restart.
    /// </summary>
    public sealed class DatabaseWarmupService : IHostedService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseWarmupService> _logger;

        public DatabaseWarmupService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseWarmupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // A minimal query that costs nothing in the database but forces Npgsql
                // to open and authenticate a real connection, priming the pool.
                await db.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

                _logger.LogInformation("Database connection pool warmed up successfully.");
            }
            catch (Exception ex)
            {
                // Non-fatal: if the DB is unreachable at startup we log a warning and
                // continue. The first user request will surface the real error; we must
                // not block the app from starting due to a transient DB hiccup.
                _logger.LogWarning(ex, "Database warm-up query failed — first request may be slower than usual.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
