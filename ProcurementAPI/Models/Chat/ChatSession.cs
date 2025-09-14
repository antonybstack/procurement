using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace ProcurementAPI.Models.Chat;

/// <summary>
/// Represents a chat session with persistent conversation history
/// </summary>
[Table("chat_sessions")]
public class ChatSession
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(255)]
    public string? UserId { get; set; }

    [MaxLength(500)]
    public string? Title { get; set; }

    /// <summary>
    /// JSON array of chat messages with structure: {role, content, timestamp}
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string Messages { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Optional metadata like model used, total tokens, etc.
    /// </summary>
    [Column(TypeName = "jsonb")]
    public string? Metadata { get; set; }

    /// <summary>
    /// Deserializes the Messages JSON into a list of ChatMessage objects
    /// </summary>
    [NotMapped]
    public List<ChatMessage> MessageList
    {
        get
        {
            if (string.IsNullOrEmpty(Messages) || Messages == "[]")
                return new List<ChatMessage>();

            try
            {
                return JsonSerializer.Deserialize<List<ChatMessage>>(Messages) ?? new List<ChatMessage>();
            }
            catch (JsonException)
            {
                return new List<ChatMessage>();
            }
        }
        set
        {
            Messages = JsonSerializer.Serialize(value);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Represents a single chat message in the conversation
/// </summary>
public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ChatMessage() { }

    public ChatMessage(string role, string content)
    {
        Role = role;
        Content = content;
        Timestamp = DateTime.UtcNow;
    }
}