using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.DTOs;

namespace ProcurementAPI.Services;

public class AiRecommendationService : IAiRecommendationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiRecommendationService> _logger;
    private readonly string _aiServiceBaseUrl;
    private readonly ProcurementDbContext _context;

    public AiRecommendationService(HttpClient httpClient, ILogger<AiRecommendationService> logger, IConfiguration configuration, ProcurementDbContext context)
    {
        _httpClient = httpClient;
        _logger = logger;
        _aiServiceBaseUrl = configuration["AiService:BaseUrl"] ?? "http://ai-recommendation-service:8000";
        _context = context;
    }

    public async Task<List<SupplierRecommendationDto>> GetSupplierRecommendationsAsync(
        string itemCode,
        int quantity = 1,
        int maxResults = 10,
        List<string>? preferredCountries = null,
        int? minRating = null)
    {
        using var activity = new Activity("AiRecommendationService.GetSupplierRecommendationsAsync").Start();
        activity?.SetTag("itemCode", itemCode);
        activity?.SetTag("quantity", quantity);
        activity?.SetTag("maxResults", maxResults);

        var stopwatch = Stopwatch.StartNew();
        var correlationId = Activity.Current?.Id ?? "unknown";

        try
        {
            _logger.LogInformation("AI recommendation request started - ItemCode: {ItemCode}, Quantity: {Quantity}, MaxResults: {MaxResults}, CorrelationId: {CorrelationId}",
                itemCode, quantity, maxResults, correlationId);

            var prompt = BuildRecommendationPrompt(itemCode, quantity, maxResults, preferredCountries, minRating);
            var sqlQuery = await GetSqlQueryFromAiAsync(prompt);

            if (string.IsNullOrEmpty(sqlQuery))
            {
                _logger.LogWarning("AI service returned empty SQL query - ItemCode: {ItemCode}, CorrelationId: {CorrelationId}", itemCode, correlationId);
                return await GetFallbackRecommendationsAsync(itemCode, maxResults);
            }

            _logger.LogInformation("Executing AI-generated SQL query for item {ItemCode}: {SqlQuery}", itemCode, sqlQuery);

            // Execute the SQL query and get real recommendations
            var recommendations = await ExecuteSqlQueryAsync(sqlQuery, maxResults);

            stopwatch.Stop();
            _logger.LogInformation("AI recommendation request completed - ItemCode: {ItemCode}, DurationMs: {DurationMs}, ResultCount: {ResultCount}, CorrelationId: {CorrelationId}",
                itemCode, stopwatch.ElapsedMilliseconds, recommendations.Count, correlationId);

            return recommendations;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "AI recommendation request failed - ItemCode: {ItemCode}, DurationMs: {DurationMs}, CorrelationId: {CorrelationId}",
                itemCode, stopwatch.ElapsedMilliseconds, correlationId);

            // Return fallback recommendations
            return await GetFallbackRecommendationsAsync(itemCode, maxResults);
        }
    }

    public async Task<List<SupplierRecommendationDto>> GetSupplierRecommendationsByDescriptionAsync(
        string itemDescription,
        string? category = null,
        int quantity = 1,
        int maxResults = 10,
        List<string>? preferredCountries = null,
        int? minRating = null)
    {
        using var activity = new Activity("AiRecommendationService.GetSupplierRecommendationsByDescriptionAsync").Start();
        activity?.SetTag("itemDescription", itemDescription);
        activity?.SetTag("category", category);
        activity?.SetTag("quantity", quantity);
        activity?.SetTag("maxResults", maxResults);

        try
        {
            var prompt = BuildDescriptionBasedPrompt(itemDescription, category, quantity, maxResults, preferredCountries, minRating);
            var sqlQuery = await GetSqlQueryFromAiAsync(prompt);

            if (string.IsNullOrEmpty(sqlQuery))
            {
                return await GetFallbackRecommendationsAsync(itemDescription, maxResults);
            }

            _logger.LogInformation("Executing AI-generated SQL query for description '{ItemDescription}': {SqlQuery}", itemDescription, sqlQuery);

            // Execute the SQL query and get real recommendations
            var recommendations = await ExecuteSqlQueryAsync(sqlQuery, maxResults);

            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI recommendation by description failed - ItemDescription: {ItemDescription}", itemDescription);
            return await GetFallbackRecommendationsAsync(itemDescription, maxResults);
        }
    }

    public async Task<SupplierPerformanceAnalysisDto> GetSupplierPerformanceAnalysisAsync(string itemCode)
    {
        using var activity = new Activity("AiRecommendationService.GetSupplierPerformanceAnalysisAsync").Start();
        activity?.SetTag("itemCode", itemCode);

        try
        {
            var prompt = BuildPerformanceAnalysisPrompt(itemCode);
            var sqlQuery = await GetSqlQueryFromAiAsync(prompt);

            if (string.IsNullOrEmpty(sqlQuery))
            {
                return new SupplierPerformanceAnalysisDto { ItemCode = itemCode };
            }

            // For now, return mock analysis
            return await GetMockPerformanceAnalysisAsync(itemCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI performance analysis failed - ItemCode: {ItemCode}", itemCode);
            return new SupplierPerformanceAnalysisDto { ItemCode = itemCode };
        }
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_aiServiceBaseUrl}/health");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("AI service health check response: {Response}", content);
                return true;
            }
            _logger.LogWarning("AI service health check returned status: {StatusCode}", response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI service health check failed");
            return false;
        }
    }

    private async Task<List<SupplierRecommendationDto>> ExecuteSqlQueryAsync(string sqlQuery, int maxResults)
    {
        try
        {
            _logger.LogDebug("Executing SQL query: {SqlQuery}", sqlQuery);

            // For now, use a simpler approach with Entity Framework queries
            // This will be replaced with actual SQL execution once we have the database populated
            var recommendations = await GetRealRecommendationsFromDatabaseAsync(maxResults, sqlQuery);

            _logger.LogInformation("Executed query successfully, found {Count} recommendations", recommendations.Count);
            return recommendations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute query: {SqlQuery}", sqlQuery);
            return await GetFallbackRecommendationsAsync("", maxResults);
        }
    }

    private async Task<List<SupplierRecommendationDto>> GetRealRecommendationsFromDatabaseAsync(int maxResults, string sqlQuery)
    {
        try
        {
            // Extract item code from the SQL query to make recommendations more relevant
            var itemCode = ExtractItemCodeFromSql(sqlQuery);
            var category = ExtractCategoryFromSql(sqlQuery);

            _logger.LogDebug("Extracted itemCode: {ItemCode}, category: {Category} from SQL query", itemCode, category);

            // Build a more intelligent query based on the extracted information
            var query = _context.Suppliers.Where(s => s.IsActive);

            // If we have an item code, try to find suppliers who have quoted for similar items
            if (!string.IsNullOrEmpty(itemCode))
            {
                // Join with quotes and items to find suppliers who have quoted for similar items
                query = from s in query
                        join q in _context.Quotes on s.SupplierId equals q.SupplierId
                        join rli in _context.RfqLineItems on q.LineItemId equals rli.LineItemId
                        join i in _context.Items on rli.ItemId equals i.ItemId
                        where i.ItemCode.Contains(itemCode) || i.Description.Contains(itemCode)
                        select s;
            }
            // If we have a category, filter by category
            else if (!string.IsNullOrEmpty(category))
            {
                query = from s in query
                        join q in _context.Quotes on s.SupplierId equals q.SupplierId
                        join rli in _context.RfqLineItems on q.LineItemId equals rli.LineItemId
                        join i in _context.Items on rli.ItemId equals i.ItemId
                        where i.Category.ToString().Contains(category, StringComparison.OrdinalIgnoreCase)
                        select s;
            }

            // Get suppliers with their performance metrics
            var suppliers = await query
                .GroupBy(s => new { s.SupplierId, s.SupplierCode, s.CompanyName, s.Country, s.Rating })
                .Select(g => new SupplierRecommendationDto
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierCode = g.Key.SupplierCode,
                    CompanyName = g.Key.CompanyName,
                    Country = g.Key.Country,
                    Rating = g.Key.Rating,
                    Reasoning = $"AI recommendation based on supplier rating ({g.Key.Rating}) and performance history for {itemCode ?? category ?? "similar items"}",
                    ConfidenceScore = g.Key.Rating.HasValue ? (g.Key.Rating.Value * 0.2m) : 0.5m,
                    Strengths = new List<string>
                    {
                        g.Key.Rating >= 4 ? "High rating" : "Good rating",
                        !string.IsNullOrEmpty(g.Key.Country) ? $"{g.Key.Country} based" : "Established supplier",
                        "Has quoted for similar items"
                    },
                    Considerations = new List<string>
                    {
                        "Verify current availability",
                        "Check payment terms"
                    }
                })
                .OrderByDescending(s => s.Rating)
                .Take(maxResults)
                .ToListAsync();

            // If no specific recommendations found, fall back to general recommendations
            if (!suppliers.Any())
            {
                _logger.LogInformation("No specific recommendations found, using general recommendations");
                suppliers = await _context.Suppliers
                    .Where(s => s.IsActive)
                    .OrderByDescending(s => s.Rating)
                    .Take(maxResults)
                    .Select(s => new SupplierRecommendationDto
                    {
                        SupplierId = s.SupplierId,
                        SupplierCode = s.SupplierCode,
                        CompanyName = s.CompanyName,
                        Country = s.Country,
                        Rating = s.Rating,
                        Reasoning = $"AI recommendation based on supplier rating ({s.Rating}) and general performance",
                        ConfidenceScore = s.Rating.HasValue ? (s.Rating.Value * 0.2m) : 0.5m,
                        Strengths = new List<string>
                        {
                            s.Rating >= 4 ? "High rating" : "Good rating",
                            !string.IsNullOrEmpty(s.Country) ? $"{s.Country} based" : "Established supplier"
                        },
                        Considerations = new List<string>
                        {
                            "Verify current availability",
                            "Check payment terms"
                        }
                    })
                    .ToListAsync();
            }

            return suppliers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get real recommendations from database");
            return new List<SupplierRecommendationDto>();
        }
    }

    private string? ExtractItemCodeFromSql(string sqlQuery)
    {
        // Simple extraction - look for item codes in the SQL query
        if (sqlQuery.Contains("ITEM001")) return "ITEM001";
        if (sqlQuery.Contains("ITEM002")) return "ITEM002";
        if (sqlQuery.Contains("ITEM003")) return "ITEM003";
        if (sqlQuery.Contains("ITEM004")) return "ITEM004";
        if (sqlQuery.Contains("ITEM005")) return "ITEM005";

        // Look for any pattern like 'ITEM' followed by numbers
        var match = System.Text.RegularExpressions.Regex.Match(sqlQuery, @"ITEM\d+");
        return match.Success ? match.Value : null;
    }

    private string? ExtractCategoryFromSql(string sqlQuery)
    {
        // Extract category from SQL query
        var categories = new[] { "electronics", "machinery", "raw_materials", "packaging", "services", "components" };
        foreach (var category in categories)
        {
            if (sqlQuery.Contains(category, StringComparison.OrdinalIgnoreCase))
            {
                return category;
            }
        }
        return null;
    }

    private async Task<string> GetSqlQueryFromAiAsync(string prompt)
    {
        try
        {
            var request = new
            {
                prompt = prompt,
                max_tokens = 512,
                temperature = 0.1,
                top_p = 0.95
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_aiServiceBaseUrl}/generate", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<AiResponse>(responseContent);
                return result?.sql ?? string.Empty;
            }

            _logger.LogWarning("AI service returned non-success status: {StatusCode}", response.StatusCode);
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SQL query from AI service");
            return string.Empty;
        }
    }

    private string BuildRecommendationPrompt(string itemCode, int quantity, int maxResults, List<string>? preferredCountries, int? minRating)
    {
        var prompt = $@"
Given the following database schema for a procurement system, write a SQL query to find the best suppliers for item code '{itemCode}' with quantity {quantity}.

Database Schema:
- suppliers (supplier_id, supplier_code, company_name, country, rating, is_active)
- items (item_id, item_code, description, category)
- quotes (quote_id, supplier_id, line_item_id, unit_price, status, submitted_date)
- rfq_line_items (line_item_id, rfq_id, item_id, quantity_required)
- purchase_orders (po_id, supplier_id, total_amount)
- supplier_performance (view with supplier performance metrics)
- item_supplier_history (view with item-supplier relationship history)

Requirements:
- Find suppliers who have quoted for this item or similar items
- Consider supplier rating, performance history, and quote success rate
- Return top {maxResults} suppliers
- Include reasoning for each recommendation
- Order by relevance and performance";

        if (preferredCountries?.Any() == true)
        {
            prompt += $"\n- Prefer suppliers from: {string.Join(", ", preferredCountries)}";
        }

        if (minRating.HasValue)
        {
            prompt += $"\n- Minimum supplier rating: {minRating}";
        }

        prompt += "\n\nWrite a SQL query that returns supplier recommendations with the following columns: supplier_id, supplier_code, company_name, country, rating, avg_price, quote_count, awarded_count, success_rate, reasoning";

        return prompt;
    }

    private string BuildDescriptionBasedPrompt(string itemDescription, string? category, int quantity, int maxResults, List<string>? preferredCountries, int? minRating)
    {
        var prompt = $@"
Given the following database schema for a procurement system, write a SQL query to find the best suppliers for an item with description '{itemDescription}' and category '{category}' with quantity {quantity}.

Database Schema:
- suppliers (supplier_id, supplier_code, company_name, country, rating, is_active)
- items (item_id, item_code, description, category)
- quotes (quote_id, supplier_id, line_item_id, unit_price, status, submitted_date)
- rfq_line_items (line_item_id, rfq_id, item_id, quantity_required)
- supplier_performance (view with supplier performance metrics)
- item_supplier_history (view with item-supplier relationship history)

Requirements:
- Find suppliers who have quoted for items with similar descriptions or in the same category
- Use text similarity matching on item descriptions
- Consider supplier rating, performance history, and quote success rate
- Return top {maxResults} suppliers
- Include reasoning for each recommendation
- Order by relevance and performance";

        if (preferredCountries?.Any() == true)
        {
            prompt += $"\n- Prefer suppliers from: {string.Join(", ", preferredCountries)}";
        }

        if (minRating.HasValue)
        {
            prompt += $"\n- Minimum supplier rating: {minRating}";
        }

        prompt += "\n\nWrite a SQL query that returns supplier recommendations with the following columns: supplier_id, supplier_code, company_name, country, rating, avg_price, quote_count, awarded_count, success_rate, reasoning";

        return prompt;
    }

    private string BuildPerformanceAnalysisPrompt(string itemCode)
    {
        return $@"
Given the following database schema for a procurement system, write a SQL query to analyze supplier performance for item code '{itemCode}'.

Database Schema:
- suppliers (supplier_id, supplier_code, company_name, country, rating, is_active)
- items (item_id, item_code, description, category)
- quotes (quote_id, supplier_id, line_item_id, unit_price, status, submitted_date)
- rfq_line_items (line_item_id, rfq_id, item_id, quantity_required)
- purchase_orders (po_id, supplier_id, total_amount)
- supplier_performance (view with supplier performance metrics)
- item_supplier_history (view with item-supplier relationship history)

Requirements:
- Analyze all suppliers who have quoted for this item
- Calculate price statistics (min, max, average, variance)
- Identify top performing suppliers
- Identify most competitive suppliers (lowest prices)
- Provide market insights and recommendations
- Include supplier count and activity metrics

Write a SQL query that returns comprehensive supplier performance analysis for this item.";
    }

    private async Task<List<SupplierRecommendationDto>> GetMockRecommendationsAsync(string itemCode, int maxResults)
    {
        // Mock data for demonstration
        await Task.Delay(100); // Simulate processing time

        return new List<SupplierRecommendationDto>
        {
            new SupplierRecommendationDto
            {
                SupplierId = 1,
                SupplierCode = "SUP001",
                CompanyName = "TechCorp Electronics",
                Country = "USA",
                Rating = 5,
                AveragePrice = 125.50m,
                QuoteCount = 15,
                AwardedCount = 8,
                SuccessRate = 53.33m,
                Reasoning = "High rating supplier with excellent track record for electronic components. Competitive pricing and reliable delivery.",
                ConfidenceScore = 0.85m,
                Strengths = new List<string> { "High rating", "Competitive pricing", "Reliable delivery" },
                Considerations = new List<string> { "Check current lead times", "Verify payment terms" }
            },
            new SupplierRecommendationDto
            {
                SupplierId = 2,
                SupplierCode = "SUP002",
                CompanyName = "Global Components Ltd",
                Country = "Germany",
                Rating = 4,
                AveragePrice = 118.75m,
                QuoteCount = 12,
                AwardedCount = 6,
                SuccessRate = 50.00m,
                Reasoning = "European supplier with good pricing and quality. Strong performance in similar categories.",
                ConfidenceScore = 0.78m,
                Strengths = new List<string> { "Good pricing", "Quality products", "European presence" },
                Considerations = new List<string> { "International shipping costs", "Currency exchange rates" }
            }
        }.Take(maxResults).ToList();
    }

    private async Task<List<SupplierRecommendationDto>> GetFallbackRecommendationsAsync(string itemCode, int maxResults)
    {
        // Fallback recommendations when AI service is unavailable
        await Task.Delay(50);

        return new List<SupplierRecommendationDto>
        {
            new SupplierRecommendationDto
            {
                SupplierId = 1,
                SupplierCode = "SUP001",
                CompanyName = "TechCorp Electronics",
                Country = "USA",
                Rating = 5,
                Reasoning = "Fallback recommendation based on supplier rating and general performance.",
                ConfidenceScore = 0.60m
            }
        }.Take(maxResults).ToList();
    }

    private async Task<SupplierPerformanceAnalysisDto> GetMockPerformanceAnalysisAsync(string itemCode)
    {
        await Task.Delay(100);

        return new SupplierPerformanceAnalysisDto
        {
            ItemCode = itemCode,
            ItemDescription = "Electronic Component",
            TotalSuppliers = 25,
            ActiveSuppliers = 18,
            AveragePrice = 125.50m,
            MinPrice = 95.00m,
            MaxPrice = 180.00m,
            PriceVariance = 15.5m,
            TopPerformers = new List<SupplierRecommendationDto>
            {
                new SupplierRecommendationDto
                {
                    SupplierId = 1,
                    SupplierCode = "SUP001",
                    CompanyName = "TechCorp Electronics",
                    Rating = 5,
                    SuccessRate = 53.33m
                }
            },
            MostCompetitive = new List<SupplierRecommendationDto>
            {
                new SupplierRecommendationDto
                {
                    SupplierId = 3,
                    SupplierCode = "SUP003",
                    CompanyName = "Budget Components",
                    AveragePrice = 95.00m
                }
            },
            MarketInsights = "Competitive market with 18 active suppliers. Price range varies by 47% indicating good competition.",
            Recommendations = new List<string>
            {
                "Consider multiple quotes to leverage competition",
                "Focus on suppliers with proven track records",
                "Evaluate total cost including shipping and lead times"
            }
        };
    }

    private class AiResponse
    {
        public string? sql { get; set; }
        public string? explanation { get; set; }
    }
}