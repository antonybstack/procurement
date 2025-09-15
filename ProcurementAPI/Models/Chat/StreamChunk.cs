using System.Text.Json;

namespace ProcurementAPI.Models.Chat;

/// <summary>
/// Represents a streaming chunk for Server-Sent Events
/// </summary>
public class StreamChunk
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "content";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ToolName { get; set; }
    public ProgressStatus? Status { get; set; }

    /// <summary>
    /// Creates a content chunk for AI response text
    /// </summary>
    public static StreamChunk CreateContent(string content)
    {
        return new StreamChunk
        {
            Type = "content",
            Content = content
        };
    }

    /// <summary>
    /// Creates a progress chunk for tool call updates
    /// </summary>
    public static StreamChunk CreateProgress(string content, string? toolName = null, ProgressStatus status = ProgressStatus.InProgress)
    {
        return new StreamChunk
        {
            Type = "progress",
            Content = content,
            ToolName = toolName,
            Status = status
        };
    }

    /// <summary>
    /// Creates a progress chunk from a ProgressMessage
    /// </summary>
    public static StreamChunk FromProgressMessage(ProgressMessage progressMessage)
    {
        return new StreamChunk
        {
            Id = progressMessage.Id,
            Type = "progress",
            Content = progressMessage.Content,
            CreatedAt = progressMessage.CreatedAt,
            ToolName = progressMessage.ToolName,
            Status = progressMessage.Status
        };
    }

    /// <summary>
    /// Serializes the chunk to JSON for SSE
    /// </summary>
    public string ToJsonString()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(this, options);
    }

    /// <summary>
    /// Formats the chunk for Server-Sent Events
    /// </summary>
    public string ToSseFormat()
    {
        return $"data: {ToJsonString()}\n\n";
    }
}