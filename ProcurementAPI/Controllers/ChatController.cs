using System.ComponentModel;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using ProcurementAPI.Models;
using ProcurementAPI.Services;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatClient _chatClient;
    private readonly SemanticSearch _search;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatClient chatClient,
        SemanticSearch search,
        ILogger<ChatController> logger)
    {
        _chatClient = chatClient;
        _search = search;
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
                Tools = [AIFunctionFactory.Create(SearchAsync)]
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
                Tools = [AIFunctionFactory.Create(SearchAsync)]
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
    /// Preserve existing search functionality for function calling
    /// </summary>
    [Description("Searches for information using a phrase or keyword")]
    private async Task<IEnumerable<string>> SearchAsync(
        [Description("The phrase to search for.")] string searchPhrase,
        [Description("If possible, specify the filename to search that file only.")] string? filenameFilter = null)
    {
        var results = await _search.SearchAsync(searchPhrase, filenameFilter, maxResults: 5);
        return results.Select(result =>
            $"<result filename=\"{result.DocumentId}\" page_number=\"{result.PageNumber}\">{result.Text}</result>");
    }

    /// <summary>
    /// Get the system prompt for RAG functionality
    /// </summary>
    private static string GetSystemPrompt()
    {
        return @"
            You are an assistant who answers questions about information you retrieve.
            If the question is vague or ambiguous, you can assume the question is about the documents you have access to.
            Use only simple markdown to format your responses.

            Use the search tool to find relevant information. When you do this, end your
            reply with citations in the special XML format:

            <citation filename='string' page_number='number'>exact quote here</citation>

            Always include the citation in your response if there are results.

            The quote must be max 10 words, taken word-for-word from the search result, and is the basis for why the citation is relevant.
            Don't refer to the presence of citations; just emit these tags right at the end, with no surrounding text.";
    }
}
