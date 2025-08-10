using System.ComponentModel.DataAnnotations;

namespace ProcurementAPI.Models;

/// <summary>
/// Public-facing chat request that limits what clients can control
/// </summary>
public record PublicChatRequest(
    [Required] string Message)
{
    public PublicChatRequest() : this(string.Empty) { }
}

/// <summary>
/// Simple response for public chat endpoints
/// </summary>
public record PublicChatResponse(
    string Id,
    string Content,
    DateTime CreatedAt)
{
    public PublicChatResponse() : this(string.Empty, string.Empty, DateTime.MinValue) { }
}
