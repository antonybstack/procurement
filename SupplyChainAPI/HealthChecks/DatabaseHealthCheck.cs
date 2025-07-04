using Microsoft.Extensions.Diagnostics.HealthChecks;
using SupplyChainAPI.Data;

namespace SupplyChainAPI.HealthChecks
{
    public class DatabaseHealthCheck : IHealthCheck
    {
        private readonly SupplyChainDbContext _context;
        private readonly ILogger<DatabaseHealthCheck> _logger;

        public DatabaseHealthCheck(SupplyChainDbContext context, ILogger<DatabaseHealthCheck> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.Database.CanConnectAsync(cancellationToken);
                return HealthCheckResult.Healthy("Database is accessible");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Database health check failed");
                return HealthCheckResult.Unhealthy("Database is not accessible", ex);
            }
        }
    }
}