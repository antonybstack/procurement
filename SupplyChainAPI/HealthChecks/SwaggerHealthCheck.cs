using System.Net.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SupplyChainAPI.HealthChecks
{
    public class SwaggerHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SwaggerHealthCheck> _logger;

        public SwaggerHealthCheck(IHttpClientFactory httpClientFactory, ILogger<SwaggerHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);

                var response = await client.GetAsync("http://localhost:8080/swagger/index.html", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Healthy("Swagger UI is available");
                }

                return HealthCheckResult.Unhealthy($"Swagger UI returned status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Swagger health check failed");
                return HealthCheckResult.Unhealthy("Swagger UI is not accessible", ex);
            }
        }
    }
}