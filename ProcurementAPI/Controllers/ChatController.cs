using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<ChatController> _logger;
    private readonly ISupplierDataService _supplierDataService;
    private readonly ISupplierService _supplierService;
    private readonly ISupplierVectorService _supplierVectorService;

    public ChatController(
        IChatClient chatClient,
        ISupplierService supplierService,
        ISupplierDataService supplierDataService,
        ISupplierVectorService supplierVectorService,
        ILogger<ChatController> logger)
    {
        _chatClient = chatClient;
        _supplierService = supplierService;
        _supplierDataService = supplierDataService;
        _supplierVectorService = supplierVectorService;
        _logger = logger;
    }

    /// <summary>
    ///     Creates a non-streaming chat completion
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
                Tools =
                [
                    AIFunctionFactory.Create(GetSupplierInfoAsync),
                    AIFunctionFactory.Create(SearchSuppliersVectorAsync),
                    AIFunctionFactory.Create(SearchSuppliersKeywordAsync)
                ],
                AllowMultipleToolCalls = true
            };

            // For non-streaming, we'll collect the streaming response
            var responseBuilder = new StringBuilder();
            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions))
            {
                responseBuilder.Append(update.Text);
            }

            return Ok(new PublicChatResponse(
                Guid.NewGuid().ToString(),
                responseBuilder.ToString(),
                DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating chat completion");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    ///     Creates a streaming chat completion using Server-Sent Events
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
                Tools =
                [
                    AIFunctionFactory.Create(GetSupplierInfoAsync),
                    AIFunctionFactory.Create(SearchSuppliersVectorAsync),
                    AIFunctionFactory.Create(SearchSuppliersKeywordAsync)
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
                {
                    break;
                }
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
    ///     Gets detailed information about a specific supplier
    /// </summary>
    [Description("Gets detailed information about a specific supplier including contact details, capabilities, and performance data")]
    private async Task<string> GetSupplierInfoAsync(
        [Description("The supplier name or supplier code to search for.")]
        string supplierNameOrCode)
    {
        try
        {
            // First search for suppliers by name/code
            PaginatedResult<SupplierDto> searchResults = await _supplierService.GetSuppliersAsync(
                1,
                10,
                supplierNameOrCode,
                null,
                null,
                true);

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
            {
                response.AppendLine($"- Contact: {detailedInfo.ContactName}");
            }

            if (!string.IsNullOrEmpty(detailedInfo.Email))
            {
                response.AppendLine($"- Email: {detailedInfo.Email}");
            }

            if (!string.IsNullOrEmpty(detailedInfo.Phone))
            {
                response.AppendLine($"- Phone: {detailedInfo.Phone}");
            }

            // Location information
            var location = new List<string>();
            if (!string.IsNullOrEmpty(detailedInfo.City))
            {
                location.Add(detailedInfo.City);
            }

            if (!string.IsNullOrEmpty(detailedInfo.State))
            {
                location.Add(detailedInfo.State);
            }

            if (!string.IsNullOrEmpty(detailedInfo.Country))
            {
                location.Add(detailedInfo.Country);
            }

            if (location.Count > 0)
            {
                response.AppendLine($"- Location: {string.Join(", ", location)}");
            }

            if (detailedInfo.Rating.HasValue)
            {
                response.AppendLine($"- Rating: {detailedInfo.Rating}/5");
            }

            if (!string.IsNullOrEmpty(detailedInfo.PaymentTerms))
            {
                response.AppendLine($"- Payment Terms: {detailedInfo.PaymentTerms}");
            }

            if (detailedInfo.CreditLimit.HasValue)
            {
                response.AppendLine($"- Credit Limit: ${detailedInfo.CreditLimit:N2}");
            }

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
                {
                    response.AppendLine($"- Average Quote Price: ${detailedInfo.Performance.AvgQuotePrice:N2}");
                }

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
    ///     Searches for suppliers using AI-powered semantic vector search
    /// </summary>
    [Description("Searches for suppliers using AI-powered semantic vector search to find suppliers based on meaning and context rather than exact keyword matches")]
    private async Task<string> SearchSuppliersVectorAsync(
        [Description("The search query or description of what you're looking for in suppliers (e.g., 'metal fabrication companies', 'electronics manufacturers', 'sustainable packaging suppliers')")]
        string searchQuery,
        [Description("Number of suppliers to return (1-20, defaults to 10)")]
        int limit = 10)
    {
        try
        {
            // Ensure limit is within bounds
            limit = Math.Max(1, Math.Min(limit, 20));

            IAsyncEnumerable<Supplier> results = await _supplierVectorService.SearchByVectorAsync(searchQuery, limit, CancellationToken.None);
            List<Supplier> suppliers = await results.ToListAsync();

            if (!suppliers.Any())
            {
                return $"No suppliers found using vector search for query: '{searchQuery}'";
            }

            var response = new StringBuilder();
            response.AppendLine($"**Vector Search Results for '{searchQuery}'** ({suppliers.Count} suppliers found):\n");

            foreach (var supplier in suppliers)
            {
                response.AppendLine($"**{supplier.CompanyName}** ({supplier.SupplierCode})");
                if (!string.IsNullOrEmpty(supplier.ContactName))
                {
                    response.AppendLine($"  - Contact: {supplier.ContactName}");
                }

                if (!string.IsNullOrEmpty(supplier.Email))
                {
                    response.AppendLine($"  - Email: {supplier.Email}");
                }

                if (!string.IsNullOrEmpty(supplier.City) || !string.IsNullOrEmpty(supplier.State))
                {
                    response.AppendLine($"  - Location: {supplier.City}, {supplier.State}");
                }

                if (supplier.Rating.HasValue)
                {
                    response.AppendLine($"  - Rating: {supplier.Rating}/5");
                }

                response.AppendLine($"  - Status: {(supplier.IsActive ? "Active" : "Inactive")}");
                response.AppendLine();
            }

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing vector search for query: {SearchQuery}", searchQuery);
            return $"Error performing vector search for '{searchQuery}': {ex.Message}";
        }
    }

    /// <summary>
    ///     Searches for suppliers using keyword-based full-text search
    /// </summary>
    [Description("Searches for suppliers using keyword-based full-text search to find exact matches in supplier names, codes, contact information, and other text fields")]
    private async Task<string> SearchSuppliersKeywordAsync(
        [Description("The keyword or text to search for in supplier information (e.g., 'Acme Corp', 'SUPL001', 'john@supplier.com')")]
        string keyword,
        [Description("Number of suppliers to return (1-20, defaults to 10)")]
        int limit = 10)
    {
        try
        {
            // Ensure limit is within bounds
            limit = Math.Max(1, Math.Min(limit, 20));

            IAsyncEnumerable<Supplier> results = await _supplierVectorService.SearchByKeywordAsync(keyword, limit, CancellationToken.None);
            List<Supplier> suppliers = await results.ToListAsync();

            if (!suppliers.Any())
            {
                return $"No suppliers found using keyword search for: '{keyword}'";
            }

            var response = new StringBuilder();
            response.AppendLine($"**Keyword Search Results for '{keyword}'** ({suppliers.Count} suppliers found):\n");

            foreach (var supplier in suppliers)
            {
                response.AppendLine($"**{supplier.CompanyName}** ({supplier.SupplierCode})");
                if (!string.IsNullOrEmpty(supplier.ContactName))
                {
                    response.AppendLine($"  - Contact: {supplier.ContactName}");
                }

                if (!string.IsNullOrEmpty(supplier.Email))
                {
                    response.AppendLine($"  - Email: {supplier.Email}");
                }

                if (!string.IsNullOrEmpty(supplier.City) || !string.IsNullOrEmpty(supplier.State))
                {
                    response.AppendLine($"  - Location: {supplier.City}, {supplier.State}");
                }

                if (supplier.Rating.HasValue)
                {
                    response.AppendLine($"  - Rating: {supplier.Rating}/5");
                }

                response.AppendLine($"  - Status: {(supplier.IsActive ? "Active" : "Inactive")}");
                response.AppendLine();
            }

            return response.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing keyword search for: {Keyword}", keyword);
            return $"Error performing keyword search for '{keyword}': {ex.Message}";
        }
    }

    /// <summary>
    ///     Get the system prompt for procurement assistant
    /// </summary>
    private static string GetSystemPrompt()
    {
        return @"
            You are a procurement assistant with access to supplier information and advanced search capabilities.
            Your primary focus is helping with procurement-related questions and supplier inquiries.
            Use clear, professional language and format responses with simple markdown.

            You have access to four main tools:
            1. Supplier information tool - for getting detailed information about specific suppliers by name or code
            2. Vector search tool - for AI-powered semantic search to find suppliers based on capabilities, industry, or contextual descriptions
            3. Keyword search tool - for exact text matches in supplier names, codes, contact information, and other fields
            4. Document search tool - for finding general information in uploaded documents

            Search Strategy Guidelines:
            - For specific supplier lookups (e.g., ""Tell me about supplier XYZ"", ""What is SUPL001's rating?""), use the supplier information tool
            - For capability-based searches (e.g., ""Find metal fabrication companies"", ""Who makes electronics?"", ""Sustainable packaging suppliers""), use vector search
            - For exact text searches (e.g., ""Find suppliers with 'Acme' in the name"", ""Search for john@supplier.com""), use keyword search
            - For general questions or policies, use document search

            The vector search is particularly powerful for finding suppliers based on:
            - Industry types and capabilities
            - Product categories and specializations
            - Geographic regions
            - Business characteristics and qualifications

            PROACTIVE MULTI-TOOL USAGE:
            Always look for opportunities to use multiple tools to provide comprehensive, detailed responses:
            - After finding suppliers with search tools, proactively get detailed information using the supplier information tool
            - When users ask broad questions, use search tools first, then drill down with specific lookups
            - Combine different search methods when appropriate (e.g., vector search for capabilities + keyword search for specific terms)
            - Follow up search results with detailed supplier profiles to provide complete context

            Example: If asked ""Find suppliers who manufacture electronics"", first use vector search, then automatically get detailed information for each result using the supplier information tool to provide comprehensive profiles including capabilities, performance data, and contact details.

            Choose the most appropriate search method based on the user's query type and intent, but always consider using multiple tools to enrich your response with nested data and additional context.
            You are limited to 6 tool calls per user request, so use them wisely to maximize the value of your responses.

            CRITICAL: Provide analysis steps in REAL-TIME as continuous feedback:
            - Each analysis step should start with '→ ' and be on its own line
            - Before each tool call, announce a high-level what you're about to do, without giving details on the internal tool parameters
            - After each tool call, announce progress or what you'll do next
            - If making 5 tool calls, provide 5+ separate analysis updates
            - Always include a 'Done' line before starting your response section
            - Stream each analysis step immediately, don't batch them";
    }
}