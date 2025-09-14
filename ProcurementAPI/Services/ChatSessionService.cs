using Microsoft.EntityFrameworkCore;
using ProcurementAPI.Data;
using ProcurementAPI.Models.Chat;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for managing chat sessions and conversation persistence
/// </summary>
public class ChatSessionService : IChatSessionService
{
    private readonly ProcurementDbContext _context;
    private readonly ILogger<ChatSessionService> _logger;

    public ChatSessionService(ProcurementDbContext context, ILogger<ChatSessionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatSession> CreateSessionAsync(string? userId, string? title = null, CancellationToken cancellationToken = default)
    {
        var session = new ChatSession
        {
            UserId = userId,
            Title = title,
            Messages = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created new chat session {SessionId} for user {UserId}", session.Id, userId);
        return session;
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Chat session {SessionId} not found", sessionId);
        }

        return session;
    }

    public async Task<ChatSession?> UpdateSessionAsync(Guid sessionId, List<ChatMessage> messages, string? metadata = null, CancellationToken cancellationToken = default)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Cannot update chat session {SessionId} - not found", sessionId);
            return null;
        }

        session.MessageList = messages;
        if (metadata != null)
        {
            session.Metadata = metadata;
        }
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated chat session {SessionId} with {MessageCount} messages", sessionId, messages.Count);
        return session;
    }

    public async Task<List<ChatSession>> GetUserSessionsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var sessions = await _context.ChatSessions
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.UpdatedAt)
            .ToListAsync(cancellationToken);

        _logger.LogDebug("Retrieved {Count} chat sessions for user {UserId}", sessions.Count, userId);
        return sessions;
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Cannot delete chat session {SessionId} - not found", sessionId);
            return false;
        }

        _context.ChatSessions.Remove(session);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted chat session {SessionId}", sessionId);
        return true;
    }

    public async Task<ChatSession?> UpdateSessionTitleAsync(Guid sessionId, string title, CancellationToken cancellationToken = default)
    {
        var session = await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

        if (session == null)
        {
            _logger.LogWarning("Cannot update title for chat session {SessionId} - not found", sessionId);
            return null;
        }

        session.Title = title;
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("Updated title for chat session {SessionId} to '{Title}'", sessionId, title);
        return session;
    }
}