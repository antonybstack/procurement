using System.Text.Json.Serialization;

namespace ProcurementAPI.Models.Chat;

/// <summary>
///     Represents a progress update during tool call execution
/// </summary>
public class ProgressMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "progress";
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? ToolName { get; set; }
    public ProgressStatus Status { get; set; } = ProgressStatus.InProgress;

    public static ProgressMessage Create(string content, string? toolName = null, ProgressStatus status = ProgressStatus.InProgress)
    {
        return new ProgressMessage
        {
            Content = $"{content}\n\n",
            ToolName = toolName,
            Status = status
        };
    }
}

/// <summary>
///     Progress status enumeration
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProgressStatus
{
    Starting,
    InProgress,
    Completed,
    Error
}