using ProcurementAPI.Models.Chat;

namespace ProcurementAPI.DTOs;

/// <summary>
/// DTO for chat session information
/// </summary>
public record ChatSessionDto(
    Guid Id,
    string? UserId,
    string? Title,
    List<ChatMessageDto> Messages,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public ChatSessionDto() : this(Guid.Empty, null, null, new List<ChatMessageDto>(), DateTime.MinValue, DateTime.MinValue) { }

    /// <summary>
    /// Creates a ChatSessionDto from a ChatSession entity
    /// </summary>
    public static ChatSessionDto FromEntity(ChatSession session)
    {
        var messageDtos = session.MessageList
            .Select(m => new ChatMessageDto(m.Role, m.Content, m.Timestamp))
            .ToList();

        return new ChatSessionDto(
            session.Id,
            session.UserId,
            session.Title,
            messageDtos,
            session.CreatedAt,
            session.UpdatedAt
        );
    }
}

/// <summary>
/// DTO for chat message information
/// </summary>
public record ChatMessageDto(
    string Role,
    string Content,
    DateTime Timestamp)
{
    public ChatMessageDto() : this(string.Empty, string.Empty, DateTime.MinValue) { }
}

/// <summary>
/// DTO for creating a new chat session
/// </summary>
public record CreateChatSessionRequest(
    string? UserId = null,
    string? Title = null);

/// <summary>
/// DTO for updating chat session title
/// </summary>
public record UpdateChatSessionTitleRequest(
    string Title)
{
    public UpdateChatSessionTitleRequest() : this(string.Empty) { }
}

/// <summary>
/// DTO for chat session summary (without messages)
/// </summary>
public record ChatSessionSummaryDto(
    Guid Id,
    string? UserId,
    string? Title,
    int MessageCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public ChatSessionSummaryDto() : this(Guid.Empty, null, null, 0, DateTime.MinValue, DateTime.MinValue) { }

    /// <summary>
    /// Creates a ChatSessionSummaryDto from a ChatSession entity
    /// </summary>
    public static ChatSessionSummaryDto FromEntity(ChatSession session)
    {
        return new ChatSessionSummaryDto(
            session.Id,
            session.UserId,
            session.Title,
            session.MessageList.Count,
            session.CreatedAt,
            session.UpdatedAt
        );
    }
}