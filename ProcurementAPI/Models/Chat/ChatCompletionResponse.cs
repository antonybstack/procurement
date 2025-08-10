namespace ProcurementAPI.Models;

public record ChatCompletionResponse(
    string Id,
    string Model,
    List<ChatChoiceDto> Choices,
    long Created)
{
    public ChatCompletionResponse() : this(string.Empty, string.Empty, new List<ChatChoiceDto>(), 0) { }
}

public record ChatChoiceDto(
    int Index,
    ChatMessageDto Message,
    string? FinishReason = null)
{
    public ChatChoiceDto() : this(0, new ChatMessageDto()) { }
}

// Streaming-specific DTOs
public record StreamingChatChunk(
    string Id,
    string Model,
    long Created,
    List<StreamingChoiceDto> Choices)
{
    public StreamingChatChunk() : this(string.Empty, string.Empty, 0, new List<StreamingChoiceDto>()) { }
}

public record StreamingChoiceDto(
    int Index,
    StreamingDeltaDto Delta,
    string? FinishReason = null)
{
    public StreamingChoiceDto() : this(0, new StreamingDeltaDto()) { }
}

public record StreamingDeltaDto(string? Content = null);
