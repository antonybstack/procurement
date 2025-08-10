using System.ComponentModel.DataAnnotations;

namespace ProcurementAPI.Models;

public record DocumentUploadRequest
{
    [Required]
    public IFormFile File { get; init; } = null!;

    public bool ReprocessExisting { get; init; } = false;
}

public record DocumentUploadResponse(
    string DocumentId,
    string FileName,
    long Size,
    string Status,
    DateTime UploadedAt)
{
    public DocumentUploadResponse() : this(string.Empty, string.Empty, 0, string.Empty, DateTime.MinValue) { }
}
