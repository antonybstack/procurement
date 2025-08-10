using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.Models;
using ProcurementAPI.Services;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SearchController : ControllerBase
{
    private readonly SemanticSearch _search;
    private readonly ILogger<SearchController> _logger;

    public SearchController(SemanticSearch search, ILogger<SearchController> logger)
    {
        _search = search;
        _logger = logger;
    }

    /// <summary>
    /// Performs semantic search across ingested documents
    /// </summary>
    [HttpPost("semantic")]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var results = await _search.SearchAsync(
                request.Query,
                request.DocumentFilter,
                request.MaxResults);

            var searchResults = results.Select(r => new SearchResultDto(
                DocumentId: r.DocumentId,
                PageNumber: r.PageNumber,
                Text: r.Text,
                Similarity: 0.0f // TODO: Add similarity scoring to SemanticSearch service
            )).ToList();

            return Ok(new { results = searchResults, query = request.Query });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing semantic search for query: {Query}", request.Query);
            return StatusCode(500, new { error = "Search failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets list of available documents for search filtering
    /// </summary>
    [HttpGet("documents")]
    public async Task<IActionResult> GetDocuments()
    {
        try
        {
            // TODO: Implement document listing in SemanticSearch service
            // For now, return empty list - will be implemented in next phase
            return Ok(new { documents = new List<DocumentMetadataDto>() });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document list");
            return StatusCode(500, new { error = "Failed to get documents", details = ex.Message });
        }
    }
}
