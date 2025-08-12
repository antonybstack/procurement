using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ProcurementAPI.Models;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly SemanticSearch _search;
    private readonly ISupplierService _supplierService;
    private readonly ISupplierDataService _supplierDataService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatClient chatClient,
        SemanticSearch search,
        ISupplierService supplierService,
        ISupplierDataService supplierDataService,
        ILogger<ChatController> logger)
    {
        _chatClient = chatClient;
        _search = search;
        _supplierService = supplierService;
        _supplierDataService = supplierDataService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a non-streaming chat completion
    /// </summary>
    [HttpPost("completions")]
    public async Task<IActionResult> CreateCompletion([FromBody] PublicChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Build messages with system prompt and user message
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetSystemPrompt()),
                new(ChatRole.User, request.Message)
            };

            var chatOptions = new ChatOptions
            {
                Tools = [
                    AIFunctionFactory.Create(SearchAsync),
                    AIFunctionFactory.Create(GetSupplierInfoAsync)
                ]
            };

            // For non-streaming, we'll collect the streaming response
            var responseBuilder = new StringBuilder();
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions))
            {
                responseBuilder.Append(update.Text);
            }

            return Ok(new PublicChatResponse(
                Id: Guid.NewGuid().ToString(),
                Content: responseBuilder.ToString(),
                CreatedAt: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat completion");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Creates a streaming chat completion using Server-Sent Events
    /// </summary>
    [HttpPost("completions/stream")]
    public async Task<IActionResult> CreateStreamingCompletion([FromBody] PublicChatRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Set up Server-Sent Events headers
        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";
        Response.Headers["Access-Control-Allow-Origin"] = "*";

        try
        {
            // Build messages with system prompt and user message
            var messages = new List<ChatMessage>
            {
                new(ChatRole.System, GetSystemPrompt()),
                new(ChatRole.User, request.Message)
            };

            var chatOptions = new ChatOptions
            {
                Tools = [
                    AIFunctionFactory.Create(SearchAsync),
                    AIFunctionFactory.Create(GetSupplierInfoAsync)
                ]
            };

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, HttpContext.RequestAborted))
            {
                var streamChunk = new
                {
                    id = Guid.NewGuid().ToString(),
                    content = update.Text ?? "",
                    createdAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(streamChunk, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync();

                if (HttpContext.RequestAborted.IsCancellationRequested)
                    break;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming cancelled by client");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during streaming completion");
            var errorChunk = new { error = ex.Message };
            await Response.WriteAsync($"data: {JsonSerializer.Serialize(errorChunk)}\n\n");
        }
        finally
        {
            // Signal completion
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }

        return new EmptyResult();
    }

    /// <summary>
    /// Search for information in documents (kept for future use)
    /// </summary>
    [Description("Searches for general procurement information, policies, or best practices in documents. Use this for non-supplier specific questions.")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only.")] string? filenameFilter = null)
    {
        var results = await _search.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result => result.Text);
    }

    /// <summary>
    /// Gets detailed information about a specific supplier
    /// </summary>
    [Description("Gets detailed information about a specific supplier including contact details, capabilities, and performance data")]
    private async Task<string> GetSupplierInfoAsync(
        [Description("The supplier name or supplier code to search for.")] string supplierNameOrCode)
    {
        try
        {
            // First search for suppliers by name/code
            var searchResults = await _supplierService.GetSuppliersAsync(
                page: 1,
                pageSize: 10,
                search: supplierNameOrCode,
                country: null,
                minRating: null,
                isActive: true);

            if (searchResults.Data == null || searchResults.Data.Count == 0)
            {
                return $"No suppliers found matching '{supplierNameOrCode}'.";
            }

            // Get the first match (keeping it simple as requested)
            var firstMatch = searchResults.Data.First();

            // Get detailed information including capabilities
            var detailedInfo = await _supplierDataService.GetSupplierByIdAsync(firstMatch.SupplierId);

            if (detailedInfo == null)
            {
                return $"Supplier '{supplierNameOrCode}' was found but detailed information is not available.";
            }

            // Format the response with comprehensive supplier information
            var response = new StringBuilder();
            response.AppendLine($"**Supplier: {detailedInfo.CompanyName}**");
            response.AppendLine($"- Supplier Code: {detailedInfo.SupplierCode}");

            if (!string.IsNullOrEmpty(detailedInfo.ContactName))
                response.AppendLine($"- Contact: {detailedInfo.ContactName}");

            if (!string.IsNullOrEmpty(detailedInfo.Email))
                response.AppendLine($"- Email: {detailedInfo.Email}");

            if (!string.IsNullOrEmpty(detailedInfo.Phone))
                response.AppendLine($"- Phone: {detailedInfo.Phone}");

            // Location information
            var location = new List<string>();
            if (!string.IsNullOrEmpty(detailedInfo.City)) location.Add(detailedInfo.City);
            if (!string.IsNullOrEmpty(detailedInfo.State)) location.Add(detailedInfo.State);
            if (!string.IsNullOrEmpty(detailedInfo.Country)) location.Add(detailedInfo.Country);

            if (location.Count > 0)
                response.AppendLine($"- Location: {string.Join(", ", location)}");

            if (detailedInfo.Rating.HasValue)
                response.AppendLine($"- Rating: {detailedInfo.Rating}/5");

            if (!string.IsNullOrEmpty(detailedInfo.PaymentTerms))
                response.AppendLine($"- Payment Terms: {detailedInfo.PaymentTerms}");

            if (detailedInfo.CreditLimit.HasValue)
                response.AppendLine($"- Credit Limit: ${detailedInfo.CreditLimit:N2}");

            // Capabilities
            if (detailedInfo.Capabilities != null && detailedInfo.Capabilities.Count > 0)
            {
                response.AppendLine("\n**Capabilities:**");
                foreach (var capability in detailedInfo.Capabilities)
                {
                    response.AppendLine($"- {capability.CapabilityType}: {capability.CapabilityValue}");
                }
            }

            // Performance data (if available)
            if (detailedInfo.Performance != null)
            {
                response.AppendLine("\n**Performance:**");
                response.AppendLine($"- Total Quotes: {detailedInfo.Performance.TotalQuotes}");
                response.AppendLine($"- Awarded Quotes: {detailedInfo.Performance.AwardedQuotes}");

                if (detailedInfo.Performance.AvgQuotePrice.HasValue)
                    response.AppendLine($"- Average Quote Price: ${detailedInfo.Performance.AvgQuotePrice:N2}");

                response.AppendLine($"- Total Purchase Orders: {detailedInfo.Performance.TotalPurchaseOrders}");
            }

            response.AppendLine($"- Status: {(detailedInfo.IsActive ? "Active" : "Inactive")}");

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supplier information for '{SupplierNameOrCode}'", supplierNameOrCode);
            return $"Error retrieving supplier information for '{supplierNameOrCode}': {ex.Message}";
        }
    }

    /// <summary>
    /// Get the system prompt for procurement assistant
    /// </summary>
    private static string GetSystemPrompt()
    {
        return @"
            You are a procurement assistant with access to supplier information and documents.
            Your primary focus is helping with procurement-related questions and supplier inquiries.
            Use clear, professional language and format responses with simple markdown.

            You have access to two main tools:
            1. Supplier information tool - for getting detailed information about specific suppliers
            2. Document search tool - for finding general information in uploaded documents

            For supplier-specific questions (e.g., ""Tell me about supplier XYZ"", ""What is supplier ABC capable of?""), 
            use the supplier information tool to get comprehensive details including contact information, capabilities, 
            and performance data.

            For general questions or when looking for best practices, policies, or other documentation, 
            use the document search tool.

            Always prioritize supplier data from the database over document search when the question is about a specific supplier.";
    }
}
