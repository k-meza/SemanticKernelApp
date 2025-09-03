using API.Domain.SemanticKernel.Interfaces;
using API.Domain.Vectorization.Interfaces;
using API.Options;
using API.Repositories.PgVectorDbContext;
using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace API.Domain.Vectorization;

public sealed class PgVectorRetrievalService : IRetrievalService
{
    private readonly PgVectorDbContext _db;
    private readonly ISemanticKernelFactory _kernelFactory;
    private readonly OpenAiSettings _openAiSettings;
    private readonly VectorizationOptions _vectorizationOptions;
    private readonly ILogger<PgVectorRetrievalService> _logger;

    public PgVectorRetrievalService(
        PgVectorDbContext db,
        ISemanticKernelFactory kernelFactory,
        OpenAiSettings openAiSettings,
        VectorizationOptions vectorizationOptions,
        ILogger<PgVectorRetrievalService> logger)
    {
        _db = db;
        _kernelFactory = kernelFactory;
        _openAiSettings = openAiSettings;
        _vectorizationOptions = vectorizationOptions;
        _logger = logger;
    }

    public async Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<RetrievedChunk>();

        // Embed the query using the same embedding model/dimensions used at ingestion
        var kernel = _kernelFactory.Create(_openAiSettings.ChatModelId);
        var embedSvc = _kernelFactory.GetEmbeddingService(kernel);
        var queryEmbedding = (await embedSvc.GenerateEmbeddingsAsync(new[] { query }, ct)).FirstOrDefault();
        if (queryEmbedding.IsEmpty)
            return Array.Empty<RetrievedChunk>();

        if (queryEmbedding.Length != _vectorizationOptions.EmbeddingDimensions)
        {
            _logger.LogWarning("Query embedding dimension mismatch: got {Got}, expected {Expected}",
                queryEmbedding.Length, _vectorizationOptions.EmbeddingDimensions);
        }

        var qv = new Vector(queryEmbedding.ToArray());

        // Rank by cosine distance (smaller is closer). Requires Npgsql.Vector mapping.
        var ranked = await _db.Chunks
            .Select(c => new
            {
                c.DocId,
                c.ChunkIndex,
                c.Content,
                Score = c.Embedding!.CosineDistance(qv)
            })
            .OrderBy(x => x.Score)
            .Take(topK)
            .ToListAsync(ct);

        return ranked
            .Select(x => new RetrievedChunk(x.DocId, x.ChunkIndex, x.Content, (float)x.Score))
            .ToList();
    }
}