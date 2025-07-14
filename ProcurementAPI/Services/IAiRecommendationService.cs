using ProcurementAPI.DTOs;

namespace ProcurementAPI.Services;

public interface IAiRecommendationService
{
    /// <summary>
    /// Get AI-powered supplier recommendations for a specific item/part number
    /// </summary>
    /// <param name="itemCode">The item/part number to find suppliers for</param>
    /// <param name="quantity">Required quantity</param>
    /// <param name="maxResults">Maximum number of recommendations to return</param>
    /// <param name="preferredCountries">Optional list of preferred countries</param>
    /// <param name="minRating">Minimum supplier rating</param>
    /// <returns>List of recommended suppliers with reasoning</returns>
    Task<List<SupplierRecommendationDto>> GetSupplierRecommendationsAsync(
        string itemCode,
        int quantity = 1,
        int maxResults = 10,
        List<string>? preferredCountries = null,
        int? minRating = null);

    /// <summary>
    /// Get AI-powered supplier recommendations based on item description
    /// </summary>
    /// <param name="itemDescription">Description of the item/part</param>
    /// <param name="category">Item category</param>
    /// <param name="quantity">Required quantity</param>
    /// <param name="maxResults">Maximum number of recommendations to return</param>
    /// <param name="preferredCountries">Optional list of preferred countries</param>
    /// <param name="minRating">Minimum supplier rating</param>
    /// <returns>List of recommended suppliers with reasoning</returns>
    Task<List<SupplierRecommendationDto>> GetSupplierRecommendationsByDescriptionAsync(
        string itemDescription,
        string? category = null,
        int quantity = 1,
        int maxResults = 10,
        List<string>? preferredCountries = null,
        int? minRating = null);

    /// <summary>
    /// Get AI-powered analysis of supplier performance for a specific item
    /// </summary>
    /// <param name="itemCode">The item/part number</param>
    /// <returns>Supplier performance analysis</returns>
    Task<SupplierPerformanceAnalysisDto> GetSupplierPerformanceAnalysisAsync(int itemId);

    /// <summary>
    /// Check if the AI service is healthy and available
    /// </summary>
    /// <returns>True if the service is available</returns>
    Task<bool> IsHealthyAsync();
}

public class SupplierRecommendationDto
{
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? Country { get; set; }
    public int? Rating { get; set; }
    public decimal? AveragePrice { get; set; }
    public int QuoteCount { get; set; }
    public int AwardedCount { get; set; }
    public decimal? SuccessRate { get; set; }
    public string Reasoning { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
    public List<string> Strengths { get; set; } = new List<string>();
    public List<string> Considerations { get; set; } = new List<string>();
}

public class SupplierPerformanceAnalysisDto
{
    public string ItemCode { get; set; } = string.Empty;
    public string ItemDescription { get; set; } = string.Empty;
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public decimal PriceVariance { get; set; }
    public List<SupplierRecommendationDto> TopPerformers { get; set; } = new List<SupplierRecommendationDto>();
    public List<SupplierRecommendationDto> MostCompetitive { get; set; } = new List<SupplierRecommendationDto>();
    public string MarketInsights { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new List<string>();
}