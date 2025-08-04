using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.Models;
using ProcurementAPI.Services;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AiController : ControllerBase
{
    private readonly IAiVectorizationService _aiService;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IAiVectorizationService aiService,
        IVectorStoreService vectorStoreService,
        ILogger<AiController> logger)
    {
        _aiService = aiService;
        _vectorStoreService = vectorStoreService;
        _logger = logger;
    }

    #region Legacy Vectorization Endpoints (using EF Core)

    [HttpPost("vectorize/suppliers")]
    public async Task<IActionResult> VectorizeSuppliers(CancellationToken ct)
    {
        try
        {
            var count = await _aiService.VectorizeAllSuppliersAsync(ct);
            return Ok(new { message = $"Vectorized {count} suppliers", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize suppliers");
            return StatusCode(500, new { error = "Failed to vectorize suppliers" });
        }
    }

    [HttpPost("vectorize/items")]
    public async Task<IActionResult> VectorizeItems()
    {
        try
        {
            var count = await _aiService.VectorizeAllItemsAsync();
            return Ok(new { message = $"Vectorized {count} items", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize items");
            return StatusCode(500, new { error = "Failed to vectorize items" });
        }
    }

    [HttpGet("search/suppliers")]
    public async Task<IActionResult> SearchSuppliers([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var suppliers = await _aiService.FindSimilarSuppliersAsync(query, limit);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search suppliers");
            return StatusCode(500, new { error = "Failed to search suppliers" });
        }
    }

    [HttpGet("search/items")]
    public async Task<IActionResult> SearchItems([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var items = await _aiService.FindSimilarItemsAsync(query, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search items");
            return StatusCode(500, new { error = "Failed to search items" });
        }
    }

    [HttpGet("search/semantic")]
    public async Task<IActionResult> SemanticSearch([FromQuery] string query, [FromQuery] int limit = 20)
    {
        try
        {
            var result = await _aiService.PerformSemanticSearchAsync(query, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search");
            return StatusCode(500, new { error = "Failed to perform semantic search" });
        }
    }

    #endregion

    #region New Vector Store Endpoints (using Semantic Kernel)

    [HttpPost("vectorstore/vectorize/suppliers")]
    public async Task<IActionResult> VectorizeSuppliersVectorStore()
    {
        try
        {
            var count = await _vectorStoreService.VectorizeAllSuppliersAsync();
            return Ok(new { message = $"Vectorized {count} suppliers using vector store", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize suppliers using vector store");
            return StatusCode(500, new { error = "Failed to vectorize suppliers using vector store" });
        }
    }

    [HttpPost("vectorstore/vectorize/items")]
    public async Task<IActionResult> VectorizeItemsVectorStore()
    {
        try
        {
            var count = await _vectorStoreService.VectorizeAllItemsAsync();
            return Ok(new { message = $"Vectorized {count} items using vector store", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize items using vector store");
            return StatusCode(500, new { error = "Failed to vectorize items using vector store" });
        }
    }

    [HttpPost("vectorstore/vectorize/rfqs")]
    public async Task<IActionResult> VectorizeRfqsVectorStore()
    {
        try
        {
            var count = await _vectorStoreService.VectorizeAllRfqsAsync();
            return Ok(new { message = $"Vectorized {count} RFQs using vector store", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize RFQs using vector store");
            return StatusCode(500, new { error = "Failed to vectorize RFQs using vector store" });
        }
    }

    [HttpPost("vectorstore/vectorize/quotes")]
    public async Task<IActionResult> VectorizeQuotesVectorStore()
    {
        try
        {
            var count = await _vectorStoreService.VectorizeAllQuotesAsync();
            return Ok(new { message = $"Vectorized {count} quotes using vector store", count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to vectorize quotes using vector store");
            return StatusCode(500, new { error = "Failed to vectorize quotes using vector store" });
        }
    }

    [HttpGet("vectorstore/search/suppliers")]
    public async Task<IActionResult> SearchSuppliersVectorStore([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var suppliers = await _vectorStoreService.FindSimilarSuppliersAsync(query, limit);
            return Ok(suppliers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search suppliers using vector store");
            return StatusCode(500, new { error = "Failed to search suppliers using vector store" });
        }
    }

    [HttpGet("vectorstore/search/items")]
    public async Task<IActionResult> SearchItemsVectorStore([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var items = await _vectorStoreService.FindSimilarItemsAsync(query, limit);
            return Ok(items);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search items using vector store");
            return StatusCode(500, new { error = "Failed to search items using vector store" });
        }
    }

    [HttpGet("vectorstore/search/rfqs")]
    public async Task<IActionResult> SearchRfqsVectorStore([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var rfqs = await _vectorStoreService.FindSimilarRfqsAsync(query, limit);
            return Ok(rfqs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search RFQs using vector store");
            return StatusCode(500, new { error = "Failed to search RFQs using vector store" });
        }
    }

    [HttpGet("vectorstore/search/quotes")]
    public async Task<IActionResult> SearchQuotesVectorStore([FromQuery] string query, [FromQuery] int limit = 10)
    {
        try
        {
            var quotes = await _vectorStoreService.FindSimilarQuotesAsync(query, limit);
            return Ok(quotes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search quotes using vector store");
            return StatusCode(500, new { error = "Failed to search quotes using vector store" });
        }
    }

    [HttpGet("vectorstore/search/semantic")]
    public async Task<IActionResult> SemanticSearchVectorStore([FromQuery] string query, [FromQuery] int limit = 20)
    {
        try
        {
            var result = await _vectorStoreService.PerformSemanticSearchAsync(query, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform semantic search using vector store");
            return StatusCode(500, new { error = "Failed to perform semantic search using vector store" });
        }
    }

    #endregion

    #region Health Check Endpoints

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("health/vectorstore")]
    public async Task<IActionResult> VectorStoreHealth()
    {
        try
        {
            // Try a simple search to test the vector store
            var result = await _vectorStoreService.FindSimilarSuppliersAsync("test", 1);
            return Ok(new { status = "healthy", vectorStore = "operational", timestamp = DateTime.UtcNow });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vector store health check failed");
            return StatusCode(500, new { status = "unhealthy", vectorStore = "error", error = ex.Message });
        }
    }

    #endregion
}