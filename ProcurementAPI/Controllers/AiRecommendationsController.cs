using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.Services;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiRecommendationsController : ControllerBase
{
    private readonly IAiRecommendationService _aiRecommendationService;
    private readonly ILogger<AiRecommendationsController> _logger;

    public AiRecommendationsController(IAiRecommendationService aiRecommendationService, ILogger<AiRecommendationsController> logger)
    {
        _aiRecommendationService = aiRecommendationService;
        _logger = logger;
    }

    /// <summary>
    /// Get AI-powered supplier recommendations for a specific item/part number
    /// </summary>
    [HttpGet("suppliers/{itemCode}")]
    public async Task<ActionResult<List<SupplierRecommendationDto>>> GetSupplierRecommendations(
        string itemCode,
        [FromQuery] int quantity = 1,
        [FromQuery] int maxResults = 10,
        [FromQuery] string? countries = null,
        [FromQuery] int? minRating = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(itemCode) || itemCode == "\"\"")
            {
                return BadRequest("Item code is required");
            }

            var preferredCountries = !string.IsNullOrWhiteSpace(countries)
                ? countries.Split(',').Select(c => c.Trim()).ToList()
                : null;

            var recommendations = await _aiRecommendationService.GetSupplierRecommendationsAsync(
                itemCode, quantity, maxResults, preferredCountries, minRating);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier recommendations for item {ItemCode}", itemCode);
            return StatusCode(500, "An error occurred while getting supplier recommendations");
        }
    }

    /// <summary>
    /// Get AI-powered supplier recommendations based on item description
    /// </summary>
    [HttpGet("suppliers/by-description")]
    public async Task<ActionResult<List<SupplierRecommendationDto>>> GetSupplierRecommendationsByDescription(
        [FromQuery] string description,
        [FromQuery] string? category = null,
        [FromQuery] int quantity = 1,
        [FromQuery] int maxResults = 10,
        [FromQuery] string? countries = null,
        [FromQuery] int? minRating = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return BadRequest("Item description is required");
            }

            var preferredCountries = !string.IsNullOrWhiteSpace(countries)
                ? countries.Split(',').Select(c => c.Trim()).ToList()
                : null;

            var recommendations = await _aiRecommendationService.GetSupplierRecommendationsByDescriptionAsync(
                description, category, quantity, maxResults, preferredCountries, minRating);

            return Ok(recommendations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier recommendations for description {Description}", description);
            return StatusCode(500, "An error occurred while getting supplier recommendations");
        }
    }

    /// <summary>
    /// Get AI-powered analysis of supplier performance for a specific item
    /// </summary>
    [HttpGet("analysis/{itemId:int}")]
    public async Task<ActionResult<SupplierPerformanceAnalysisDto>> GetSupplierPerformanceAnalysis(int itemId)
    {
        try
        {
            if (itemId <= 0)
            {
                return BadRequest("Item ID is required");
            }

            var analysis = await _aiRecommendationService.GetSupplierPerformanceAnalysisAsync(itemId);

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier performance analysis for item {ItemId}", itemId);
            return StatusCode(500, "An error occurred while getting supplier performance analysis");
        }
    }

    /// <summary>
    /// Check if the AI recommendation service is healthy
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult<object>> GetHealth()
    {
        try
        {
            var isHealthy = await _aiRecommendationService.IsHealthyAsync();

            return Ok(new
            {
                service = "ai-recommendations",
                status = isHealthy ? "healthy" : "unhealthy",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking AI service health");
            return StatusCode(500, new
            {
                service = "ai-recommendations",
                status = "error",
                timestamp = DateTime.UtcNow,
                error = ex.Message
            });
        }
    }
}