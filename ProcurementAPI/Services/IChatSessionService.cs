using ProcurementAPI.Models.Chat;

namespace ProcurementAPI.Services;

/// <summary>
/// Service interface for managing chat sessions and conversation persistence
/// </summary>
public interface IChatSessionService
{
    /// <summary>
    /// Creates a new chat session
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="title">Optional session title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created chat session</returns>
    Task<ChatSession> CreateSessionAsync(string? userId, string? title = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a chat session by ID
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The chat session or null if not found</returns>
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing chat session with new messages
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="messages">Complete list of messages for the session</param>
    /// <param name="metadata">Optional metadata to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated chat session</returns>
    Task<ChatSession?> UpdateSessionAsync(Guid sessionId, List<ChatMessage> messages, string? metadata = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all chat sessions for a user
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user's chat sessions</returns>
    Task<List<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a chat session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if deleted, false if not found</returns>
    Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the title of a chat session
    /// </summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="title">New title</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated chat session or null if not found</returns>
    Task<ChatSession?> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default);
}