namespace ProcurementAPI.Models;

/// <summary>
/// Server-side configuration for chat AI parameters
/// </summary>
public class ChatConfiguration
{
    public const string SectionName = "ChatSettings";

    /// <summary>
    /// Default model to use for chat completions (configured server-side)
    /// </summary>
    public string DefaultModel { get; set; } = "default";

    /// <summary>
    /// Maximum tokens per response (prevents costly requests)
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// Temperature for AI responses (0.0 = deterministic, 1.0 = creative)
    /// </summary>
    public float Temperature { get; set; } = 0.7f;

    /// <summary>
    /// Maximum results returned by semantic search
    /// </summary>
    public int MaxSearchResults { get; set; } = 5;
}
