using System.Collections.Concurrent;
using System.Threading.Channels;
using ProcurementAPI.Models.Chat;

namespace ProcurementAPI.Services;

/// <summary>
/// Service for managing conversation-scoped progress channels
/// </summary>
public class ProgressChannelService : IProgressChannelService
{
    private readonly ConcurrentDictionary<string, ChannelEntry> _channels = new();
    private readonly ILogger<ProgressChannelService> _logger;
    private readonly Timer _cleanupTimer;

    public ProgressChannelService(ILogger<ProgressChannelService> logger)
    {
        _logger = logger;

        // Setup cleanup timer to run every 10 minutes
        _cleanupTimer = new Timer(async _ => await CleanupExpiredChannelsAsync(),
            null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public Channel<ProgressMessage> GetOrCreateChannel(string conversationId)
    {
        return _channels.GetOrAdd(conversationId, _ => new ChannelEntry()).Channel;
    }

    public async Task SendProgressAsync(string conversationId, string content, string? toolName = null, ProgressStatus status = ProgressStatus.InProgress)
    {
        var message = ProgressMessage.Create(content, toolName, status);
        await SendProgressAsync(conversationId, message);
    }

    public async Task SendProgressAsync(string conversationId, ProgressMessage message)
    {
        if (_channels.TryGetValue(conversationId, out var channelEntry))
        {
            var writer = channelEntry.Channel.Writer;

            try
            {
                await writer.WriteAsync(message);
                _logger.LogDebug("Sent progress message to conversation {ConversationId}: {Content}",
                    conversationId, message.Content);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to send progress message to conversation {ConversationId} - channel may be closed",
                    conversationId);
            }
        }
        else
        {
            _logger.LogWarning("No channel found for conversation {ConversationId}", conversationId);
        }
    }

    public async Task CompleteChannelAsync(string conversationId)
    {
        if (_channels.TryRemove(conversationId, out var channelEntry))
        {
            channelEntry.Channel.Writer.Complete();
            _logger.LogDebug("Completed and removed channel for conversation {ConversationId}", conversationId);
        }
    }

    public async Task CleanupExpiredChannelsAsync()
    {
        var expiredThreshold = DateTime.UtcNow.AddHours(-1); // Cleanup channels older than 1 hour
        var expiredChannels = new List<string>();

        foreach (var kvp in _channels)
        {
            if (kvp.Value.CreatedAt < expiredThreshold)
            {
                expiredChannels.Add(kvp.Key);
            }
        }

        foreach (var conversationId in expiredChannels)
        {
            await CompleteChannelAsync(conversationId);
        }

        if (expiredChannels.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired progress channels", expiredChannels.Count);
        }

    }

    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        // Complete all remaining channels
        foreach (var kvp in _channels)
        {
            kvp.Value.Channel.Writer.Complete();
        }

        _channels.Clear();
    }

    private class ChannelEntry
    {
        public Channel<ProgressMessage> Channel { get; }
        public DateTime CreatedAt { get; }

        public ChannelEntry()
        {
            Channel = System.Threading.Channels.Channel.CreateUnbounded<ProgressMessage>();
            CreatedAt = DateTime.UtcNow;
        }
    }
}