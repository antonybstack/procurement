using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;

    public HealthController(ILogger<HealthController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint
    /// </summary>
    [HttpGet]
    public IActionResult GetHealth()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            services = new
            {
                api = "operational",
                vectorStore = "operational", // TODO: Add actual health checks
                aiServices = "operational"
            }
        });
    }

    /// <summary>
    /// Detailed status information
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        try
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                uptime = Environment.TickCount64,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                processorCount = Environment.ProcessorCount,
                workingSet = Environment.WorkingSet,
                gcMemory = GC.GetTotalMemory(false)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status");
            return StatusCode(500, new { error = "Status check failed", details = ex.Message });
        }
    }
}
