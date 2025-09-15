using System.Threading.Channels;
using ProcurementAPI.Models.Chat;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for managing conversation-scoped progress channels
/// </summary>
public interface IProgressChannelService
{
    /// <summary>
    /// Gets or creates a progress channel for the specified conversation
    /// </summary>
    Channel<ProgressMessage> GetOrCreateChannel(string conversationId);

    /// <summary>
    /// Sends a progress message to the specified conversation channel
    /// </summary>
    Task SendProgressAsync(string conversationId, string content, string? toolName = null, ProgressStatus status = ProgressStatus.InProgress);

    /// <summary>
    /// Sends a progress message to the specified conversation channel
    /// </summary>
    Task SendProgressAsync(string conversationId, ProgressMessage message);

    /// <summary>
    /// Completes and removes the channel for the specified conversation
    /// </summary>
    Task CompleteChannelAsync(string conversationId);

    /// <summary>
    /// Removes expired channels that are no longer active
    /// </summary>
    Task CleanupExpiredChannelsAsync();
}