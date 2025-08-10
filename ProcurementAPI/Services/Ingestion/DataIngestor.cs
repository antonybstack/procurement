using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using ProcurementAPI.Models;

namespace ProcurementAPI.Services.Ingestion;

public class DataIngestor(
    ILogger<DataIngestor> logger,
    VectorStoreCollection<string, IngestedChunk> chunksCollection,
    VectorStoreCollection<string, IngestedDocument> documentsCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
{
    public static async Task IngestDataAsync(IServiceProvider services, IIngestionSource source)
    {
        using var scope = services.CreateScope();
        var ingestor = scope.ServiceProvider.GetRequiredService<DataIngestor>();
        await ingestor.IngestDataAsync(source);
    }

    public async Task IngestDataAsync(IIngestionSource source)
    {
        await chunksCollection.EnsureCollectionExistsAsync();
        await documentsCollection.EnsureCollectionExistsAsync();

        var sourceId = source.SourceId;
        var documentsForSource = await documentsCollection.GetAsync(doc => doc.SourceId == sourceId, top: int.MaxValue).ToListAsync();

        var deletedDocuments = await source.GetDeletedDocumentsAsync(documentsForSource);
        foreach (var deletedDocument in deletedDocuments)
        {
            logger.LogInformation("Removing ingested data for {documentId}", deletedDocument.DocumentId);
            await DeleteChunksForDocumentAsync(deletedDocument);
            await documentsCollection.DeleteAsync(deletedDocument.Key);
        }

        var modifiedDocuments = await source.GetNewOrModifiedDocumentsAsync(documentsForSource);
        foreach (var modifiedDocument in modifiedDocuments)
        {
            logger.LogInformation("Processing {documentId}", modifiedDocument.DocumentId);
            await DeleteChunksForDocumentAsync(modifiedDocument);

            await documentsCollection.UpsertAsync(modifiedDocument);

            var newRecords = await source.CreateChunksForDocumentAsync(modifiedDocument);

            // Generate embeddings for each chunk before storing
            var chunksWithEmbeddings = new List<IngestedChunk>();
            foreach (var chunk in newRecords)
            {
                try
                {
                    // Generate embedding for the chunk text
                    var embeddings = await embeddingGenerator.GenerateAsync([chunk.Text]);
                    var embedding = embeddings.FirstOrDefault();

                    if (embedding != null && embedding.Vector.Length > 0)
                    {
                        chunk.Vector = embedding.Vector;
                        chunksWithEmbeddings.Add(chunk);
                    }
                    else
                    {
                        logger.LogWarning("Failed to generate valid embedding for chunk {chunkKey} in document {documentId}",
                            chunk.Key, chunk.DocumentId);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to generate embedding for chunk {chunkKey} in document {documentId}",
                        chunk.Key, chunk.DocumentId);
                }
            }

            if (chunksWithEmbeddings.Any())
            {
                await chunksCollection.UpsertAsync(chunksWithEmbeddings);
                logger.LogInformation("Stored {chunkCount} chunks with embeddings for document {documentId}",
                    chunksWithEmbeddings.Count, modifiedDocument.DocumentId);
            }
        }

        logger.LogInformation("Ingestion is up-to-date");

        async Task DeleteChunksForDocumentAsync(IngestedDocument document)
        {
            var documentId = document.DocumentId;
            var chunksToDelete = await chunksCollection.GetAsync(record => record.DocumentId == documentId, int.MaxValue).ToListAsync();
            if (chunksToDelete.Any())
            {
                await chunksCollection.DeleteAsync(chunksToDelete.Select(r => r.Key));
            }
        }
    }
}
