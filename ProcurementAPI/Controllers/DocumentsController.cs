using Microsoft.AspNetCore.Mvc;
using ProcurementAPI.Models;
using ProcurementAPI.Services.Ingestion;

namespace ChatApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly DataIngestor _ingestor;
    private readonly ILogger<DocumentsController> _logger;
    private readonly IWebHostEnvironment _environment;

    public DocumentsController(
        DataIngestor ingestor,
        ILogger<DocumentsController> logger,
        IWebHostEnvironment environment)
    {
        _ingestor = ingestor;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Uploads and processes a PDF document for semantic search
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument([FromForm] DocumentUploadRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest(new { error = "No file provided" });
        }

        if (!request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { error = "Only PDF files are supported" });
        }

        try
        {
            // Ensure Data directory exists
            var dataPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Data");
            Directory.CreateDirectory(dataPath);

            // Save uploaded file
            var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
            var filePath = Path.Combine(dataPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Process the document
            var source = new PDFDirectorySource(dataPath);
            await _ingestor.IngestDataAsync(source);

            return Ok(new DocumentUploadResponse(
                DocumentId: fileName,
                FileName: request.File.FileName,
                Size: request.File.Length,
                Status: "processed",
                UploadedAt: DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document: {FileName}", request.File.FileName);
            return StatusCode(500, new { error = "Upload failed", details = ex.Message });
        }
    }

    /// <summary>
    /// Gets list of all uploaded documents
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetDocuments()
    {
        try
        {
            var dataPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Data");

            if (!Directory.Exists(dataPath))
            {
                return Ok(new { documents = new List<DocumentUploadResponse>() });
            }

            var files = Directory.GetFiles(dataPath, "*.pdf");
            var documents = files.Select(file =>
            {
                var fileInfo = new FileInfo(file);
                return new DocumentUploadResponse(
                    DocumentId: fileInfo.Name,
                    FileName: fileInfo.Name,
                    Size: fileInfo.Length,
                    Status: "processed",
                    UploadedAt: fileInfo.LastWriteTime);
            }).ToList();

            return Ok(new { documents });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document list");
            return StatusCode(500, new { error = "Failed to get documents", details = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a document and its associated chunks
    /// </summary>
    [HttpDelete("{documentId}")]
    public async Task<IActionResult> DeleteDocument(string documentId)
    {
        try
        {
            var dataPath = Path.Combine(_environment.WebRootPath ?? _environment.ContentRootPath, "Data");
            var filePath = Path.Combine(dataPath, documentId);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new { error = "Document not found" });
            }

            // Delete the file
            System.IO.File.Delete(filePath);

            // TODO: Delete associated chunks from vector store
            // This will be implemented when we enhance the data layer

            return Ok(new { message = "Document deleted successfully", documentId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting document: {DocumentId}", documentId);
            return StatusCode(500, new { error = "Delete failed", details = ex.Message });
        }
    }
}
