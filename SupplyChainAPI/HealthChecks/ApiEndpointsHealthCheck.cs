using System.Net.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SupplyChainAPI.HealthChecks
{
    public class ApiEndpointsHealthCheck : IHealthCheck
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ApiEndpointsHealthCheck> _logger;

        public ApiEndpointsHealthCheck(IHttpClientFactory httpClientFactory, ILogger<ApiEndpointsHealthCheck> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);

            var endpointChecks = new List<(string name, string url, bool success, string error)>();

            // Check RFQs endpoint
            try
            {
                var response = await client.GetAsync("http://localhost:8080/api/rfqs", cancellationToken);
                endpointChecks.Add(("RFQs", "/api/rfqs", response.IsSuccessStatusCode, ""));
            }
            catch (Exception ex)
            {
                endpointChecks.Add(("RFQs", "/api/rfqs", false, ex.Message));
            }

            // Check Suppliers endpoint
            try
            {
                var response = await client.GetAsync("http://localhost:8080/api/suppliers", cancellationToken);
                endpointChecks.Add(("Suppliers", "/api/suppliers", response.IsSuccessStatusCode, ""));
            }
            catch (Exception ex)
            {
                endpointChecks.Add(("Suppliers", "/api/suppliers", false, ex.Message));
            }

            var failedEndpoints = endpointChecks.Where(e => !e.success).ToList();
            var successfulEndpoints = endpointChecks.Where(e => e.success).ToList();

            if (failedEndpoints.Any())
            {
                var failedDetails = string.Join(", ", failedEndpoints.Select(e => $"{e.name}: {e.error}"));
                _logger.LogWarning("API endpoints health check failed for: {FailedEndpoints}", failedDetails);

                return HealthCheckResult.Unhealthy(
                    $"Some API endpoints are not responding. Failed: {failedEndpoints.Count}, Successful: {successfulEndpoints.Count}",
                    data: new Dictionary<string, object>
                    {
                        ["failed_endpoints"] = failedEndpoints.Select(e => new { name = e.name, url = e.url, error = e.error }),
                        ["successful_endpoints"] = successfulEndpoints.Select(e => new { name = e.name, url = e.url })
                    });
            }

            return HealthCheckResult.Healthy(
                $"All API endpoints are responding. Total: {endpointChecks.Count}",
                data: new Dictionary<string, object>
                {
                    ["endpoints"] = endpointChecks.Select(e => new { name = e.name, url = e.url, status = "healthy" })
                });
        }
    }
}