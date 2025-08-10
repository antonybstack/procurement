using System.ComponentModel.DataAnnotations;

namespace ProcurementAPI.Models;

public record SemanticSearchRequest(
    [Required] string Query,
    string? DocumentFilter = null,
    int MaxResults = 5)
{
    public SemanticSearchRequest() : this(string.Empty) { }
}

public record SearchResultDto(
    string DocumentId,
    int PageNumber,
    string Text,
    float Similarity)
{
    public SearchResultDto() : this(string.Empty, 0, string.Empty, 0f) { }
}

public record DocumentMetadataDto(
    string Id,
    string FileName,
    DateTime UploadedAt,
    int ChunkCount,
    string Status)
{
    public DocumentMetadataDto() : this(string.Empty, string.Empty, DateTime.MinValue, 0, string.Empty) { }
}
