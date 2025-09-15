using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ProcurementAPI.DTOs;
using ProcurementAPI.Models;
using ProcurementAPI.Models.Chat;
using ProcurementAPI.Services;
using ProcurementAPI.Services.DataServices;
using ChatMessage = ProcurementAPI.Models.Chat.ChatMessage;

namespace ProcurementAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly IChatSessionService _chatSessionService;
    private readonly ILogger<ChatController> _logger;
    private readonly IProgressChannelService _progressChannelService;
    private readonly ISupplierDataService _supplierDataService;
    private readonly ISupplierService _supplierService;
    private readonly ISupplierVectorService _supplierVectorService;

    public ChatController(
        IChatClient chatClient,
        ISupplierService supplierService,
        ISupplierDataService supplierDataService,
        ISupplierVectorService supplierVectorService,
        IChatSessionService chatSessionService,
        IProgressChannelService progressChannelService,
        ILogger<ChatController> logger)
    {
        _chatClient = chatClient;
        _supplierService = supplierService;
        _supplierDataService = supplierDataService;
        _supplierVectorService = supplierVectorService;
        _chatSessionService = chatSessionService;
        _progressChannelService = progressChannelService;
        _logger = logger;
    }

    /// <summary>
    ///     Creates a streaming chat completion using Server-Sent Events with session persistence
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

        ChatSession? session = null;
        var assistantResponse = new StringBuilder();

        try
        {
            // Get session ID from headers
            var sessionIdHeader = Request.Headers["X-Chat-Session-Id"].FirstOrDefault();
            var userIdHeader = Request.Headers["X-User-Id"].FirstOrDefault();

            // Load or create session
            if (!string.IsNullOrEmpty(sessionIdHeader) && Guid.TryParse(sessionIdHeader, out var sessionId))
            {
                session = await _chatSessionService.GetSessionAsync(sessionId, HttpContext.RequestAborted);
            }

            if (session == null)
            {
                // Create new session if none provided
                session = await _chatSessionService.CreateSessionAsync(userIdHeader, cancellationToken: HttpContext.RequestAborted);
            }

            // Set session ID in response headers
            Response.Headers["X-Chat-Session-Id"] = session.Id.ToString();
            Response.Headers["Access-Control-Expose-Headers"] = "X-Chat-Session-Id";

            // Build message list from session history + system prompt + new user message
            var messages = new List<Microsoft.Extensions.AI.ChatMessage>();

            // Always start with system prompt
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.System, GetSystemPrompt()));

            // Add existing conversation history (skip system messages from history)
            foreach (var historyMessage in session.MessageList.Where(m => m.Role != "system"))
            {
                var role = historyMessage.Role.ToLowerInvariant() switch
                {
                    "user" => ChatRole.User,
                    "assistant" => ChatRole.Assistant,
                    _ => ChatRole.User
                };
                messages.Add(new Microsoft.Extensions.AI.ChatMessage(role, historyMessage.Content));
            }

            // Add new user message
            messages.Add(new Microsoft.Extensions.AI.ChatMessage(ChatRole.User, request.Message));

            var chatOptions = new ChatOptions
            {
                ConversationId = session.Id.ToString(),
                Tools =
                [
                    AIFunctionFactory.Create(GetSupplierInfoAsync),
                    AIFunctionFactory.Create(SearchSuppliersVectorAsync),
                    AIFunctionFactory.Create(SearchSuppliersKeywordAsync)
                ],
                AllowMultipleToolCalls = true
            };

            // Set conversation ID in HttpContext so tool methods can access it
            HttpContext.Items["ConversationId"] = session.Id.ToString();

            // Get or create progress channel for this conversation
            Channel<ProgressMessage> progressChannel = _progressChannelService.GetOrCreateChannel(session.Id.ToString());

            // Create cancellation token source for progress task that will be cancelled when streaming starts
            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);

            // Create two tasks: streaming response and progress monitoring
            var streamingTask = ProcessStreamingResponseAsync(messages, chatOptions, assistantResponse, progressCts, HttpContext.RequestAborted);
            var progressTask = ProcessProgressUpdatesAsync(progressChannel.Reader, progressCts.Token);

            // Process both tasks concurrently until completion
            await Task.WhenAll(streamingTask, progressTask);
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
            // Complete the progress channel
            if (session != null)
            {
                await _progressChannelService.CompleteChannelAsync(session.Id.ToString());
            }

            // Save conversation to session
            if (session != null && !HttpContext.RequestAborted.IsCancellationRequested)
            {
                try
                {
                    List<ChatMessage> updatedMessages = session.MessageList.ToList();

                    // Add user message
                    updatedMessages.Add(new ChatMessage("user", request.Message));

                    // Add assistant response if we have content
                    var assistantContent = assistantResponse.ToString().Trim();
                    if (!string.IsNullOrEmpty(assistantContent))
                    {
                        updatedMessages.Add(new ChatMessage("assistant", assistantContent));
                    }

                    await _chatSessionService.UpdateSessionAsync(session.Id, updatedMessages, cancellationToken: HttpContext.RequestAborted);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving conversation to session {SessionId}", session.Id);
                }
            }

            // Signal completion
            await Response.WriteAsync("data: [DONE]\n\n");
            await Response.Body.FlushAsync();
        }

        return new EmptyResult();
    }

    /// <summary>
    ///     Processes the streaming response from the AI client and sends content chunks via SSE
    /// </summary>
    private async Task ProcessStreamingResponseAsync(
        List<Microsoft.Extensions.AI.ChatMessage> messages,
        ChatOptions chatOptions,
        StringBuilder assistantResponse,
        CancellationTokenSource progressCts,
        CancellationToken cancellationToken)
    {
        try
        {
            var firstResponseReceived = false;

            await foreach (var update in _chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken))
            {
                // Cancel progress monitoring once AI response starts streaming
                if (!firstResponseReceived && !string.IsNullOrEmpty(update.Text))
                {
                    firstResponseReceived = true;
                    await progressCts.CancelAsync(); // Signal progress monitoring to stop
                }

                var streamChunk = StreamChunk.CreateContent(update.Text ?? "");

                // Accumulate assistant response
                if (!string.IsNullOrEmpty(update.Text))
                {
                    assistantResponse.Append(update.Text);
                }

                await Response.WriteAsync(streamChunk.ToSseFormat(), cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in streaming response processing");
            throw;
        }
    }

    /// <summary>
    ///     Processes progress updates from the channel and sends them via SSE
    /// </summary>
    private async Task ProcessProgressUpdatesAsync(
        ChannelReader<ProgressMessage> progressReader,
        CancellationToken cancellationToken)
    {
        try
        {
            while (await progressReader.WaitToReadAsync(cancellationToken))
            {
                while (progressReader.TryRead(out var progressMessage))
                {
                    var streamChunk = StreamChunk.FromProgressMessage(progressMessage);

                    await Response.WriteAsync(streamChunk.ToSseFormat(), cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in progress updates processing");
        }
    }

    /// <summary>
    ///     Gets detailed information about a specific supplier
    /// </summary>
    [Description("Gets detailed information about a specific supplier including contact details, capabilities, and performance data")]
    private async Task<string> GetSupplierInfoAsync(
        [Description("The supplier name or supplier code to search for.")]
        string supplierNameOrCode)
    {
        var conversationId = HttpContext.Items["ConversationId"]?.ToString();

        try
        {
            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Looking up supplier information for '{supplierNameOrCode}'...",
                    "GetSupplierInfo", ProgressStatus.Starting);
            }

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
                if (conversationId != null)
                {
                    await _progressChannelService.SendProgressAsync(conversationId,
                        $"→ No suppliers found matching '{supplierNameOrCode}'",
                        "GetSupplierInfo", ProgressStatus.Completed);
                }

                return $"No suppliers found matching '{supplierNameOrCode}'.";
            }

            // Get the first match (keeping it simple as requested)
            var firstMatch = searchResults.Data.First();

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Found supplier '{firstMatch.CompanyName}', getting detailed information...",
                    "GetSupplierInfo");
            }

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

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Retrieved complete information for '{detailedInfo.CompanyName}'",
                    "GetSupplierInfo", ProgressStatus.Completed);
            }

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
        var conversationId = HttpContext.Items["ConversationId"]?.ToString();

        try
        {
            // Ensure limit is within bounds
            limit = Math.Max(1, Math.Min(limit, 20));

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Performing AI-powered vector search for '{searchQuery}'...",
                    "VectorSearch", ProgressStatus.Starting);
            }

            IAsyncEnumerable<Supplier> results = await _supplierVectorService.SearchByVectorAsync(searchQuery, limit, CancellationToken.None);
            List<Supplier> suppliers = await results.ToListAsync();

            if (!suppliers.Any())
            {
                if (conversationId != null)
                {
                    await _progressChannelService.SendProgressAsync(conversationId,
                        $"→ No suppliers found for query '{searchQuery}'",
                        "VectorSearch", ProgressStatus.Completed);
                }

                return $"No suppliers found using vector search for query: '{searchQuery}'";
            }

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Found {suppliers.Count} suppliers, formatting results...",
                    "VectorSearch");
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

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Vector search completed - found {suppliers.Count} matching suppliers",
                    "VectorSearch", ProgressStatus.Completed);
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
        var conversationId = HttpContext.Items["ConversationId"]?.ToString();

        try
        {
            // Ensure limit is within bounds
            limit = Math.Max(1, Math.Min(limit, 20));

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Performing keyword search for '{keyword}'...",
                    "KeywordSearch", ProgressStatus.Starting);
            }

            IAsyncEnumerable<Supplier> results = await _supplierVectorService.SearchByKeywordAsync(keyword, limit, CancellationToken.None);
            List<Supplier> suppliers = await results.ToListAsync();

            if (!suppliers.Any())
            {
                if (conversationId != null)
                {
                    await _progressChannelService.SendProgressAsync(conversationId,
                        $"→ No suppliers found for keyword '{keyword}'",
                        "KeywordSearch", ProgressStatus.Completed);
                }

                return $"No suppliers found using keyword search for: '{keyword}'";
            }

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Found {suppliers.Count} suppliers, formatting results...",
                    "KeywordSearch");
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

            if (conversationId != null)
            {
                await _progressChannelService.SendProgressAsync(conversationId,
                    $"→ Keyword search completed - found {suppliers.Count} matching suppliers",
                    "KeywordSearch", ProgressStatus.Completed);
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
            You are limited to 6 tool calls per user request, so use them wisely to maximize the value of your responses.";
    }

    // Session Management Endpoints

    /// <summary>
    ///     Creates a new chat session
    /// </summary>
    [HttpPost("sessions")]
    public async Task<ActionResult<ChatSessionDto>> CreateSession([FromBody] CreateChatSessionRequest request)
    {
        var session = await _chatSessionService.CreateSessionAsync(request.UserId, request.Title, HttpContext.RequestAborted);
        return Ok(ChatSessionDto.FromEntity(session));
    }

    /// <summary>
    ///     Gets a specific chat session by ID
    /// </summary>
    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<ChatSessionDto>> GetSession(Guid sessionId)
    {
        var session = await _chatSessionService.GetSessionAsync(sessionId, HttpContext.RequestAborted);
        if (session == null)
        {
            return NotFound($"Session {sessionId} not found");
        }

        return Ok(ChatSessionDto.FromEntity(session));
    }

    /// <summary>
    ///     Gets all chat sessions for a user
    /// </summary>
    [HttpGet("sessions")]
    public async Task<ActionResult<List<ChatSessionSummaryDto>>> GetUserSessions([FromQuery] string? userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("UserId is required");
        }

        List<ChatSession> sessions = await _chatSessionService.GetUserSessionsAsync(userId, HttpContext.RequestAborted);
        List<ChatSessionSummaryDto> summaries = sessions.Select(ChatSessionSummaryDto.FromEntity).ToList();

        return Ok(summaries);
    }

    /// <summary>
    ///     Updates the title of a chat session
    /// </summary>
    [HttpPatch("sessions/{sessionId:guid}/title")]
    public async Task<ActionResult<ChatSessionDto>> UpdateSessionTitle(Guid sessionId, [FromBody] UpdateChatSessionTitleRequest request)
    {
        var session = await _chatSessionService.UpdateSessionTitleAsync(sessionId, request.Title, HttpContext.RequestAborted);
        if (session == null)
        {
            return NotFound($"Session {sessionId} not found");
        }

        return Ok(ChatSessionDto.FromEntity(session));
    }

    /// <summary>
    ///     Deletes a chat session
    /// </summary>
    [HttpDelete("sessions/{sessionId:guid}")]
    public async Task<IActionResult> DeleteSession(Guid sessionId)
    {
        var deleted = await _chatSessionService.DeleteSessionAsync(sessionId, HttpContext.RequestAborted);
        if (!deleted)
        {
            return NotFound($"Session {sessionId} not found");
        }

        return NoContent();
    }
}