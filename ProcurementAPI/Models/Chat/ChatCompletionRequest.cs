using System.ComponentModel.DataAnnotations;

namespace ProcurementAPI.Models;

public record ChatCompletionRequest(
    [Required] List<ChatMessageDto> Messages,
    string? Model = null,
    bool Stream = false,
    int MaxTokens = 1000,
    float Temperature = 0.7f)
{
    public ChatCompletionRequest() : this(new List<ChatMessageDto>()) { }
}

public record ChatMessageDto(
    [Required] string Role,
    [Required] string Content)
{
    public ChatMessageDto() : this(string.Empty, string.Empty) { }
}
