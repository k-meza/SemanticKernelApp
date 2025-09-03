using System.Text.Json;
using API.Domain.SemanticKernel.Interfaces;
using API.Domain.Vectorization.Interfaces;
using API.Domain.Vectorization.TextExctractor;
using API.Options;
using API.Repositories.PgVectorDbContext;
using API.Repositories.PgVectorDbContext.Entities;
using Microsoft.Extensions.Options;
using Pgvector;

namespace API.Domain.Vectorization;

public sealed class DocumentVectorizationService : IVectorizationService
{
    private readonly PgVectorDbContext _db;
    private readonly ILogger<DocumentVectorizationService> _logger;
    private readonly ISemanticKernelFactory _kernelFactory; // Provided by your existing SemanticKernelFactory
    private readonly OpenAiSettings _openAiSettings;        // Your settings record (API key + model ids)
    private readonly VectorizationOptions _opts;
    private readonly IFileTextExtractorFactory _extractorFactory;
    private readonly ITextChuncker _textChuncker;

    public DocumentVectorizationService(
        PgVectorDbContext db,
        ILogger<DocumentVectorizationService> logger,
        ISemanticKernelFactory kernelFactory,
        OpenAiSettings openAiSettings,
        IOptions<VectorizationOptions> options,
        IFileTextExtractorFactory extractorFactory,
        ITextChuncker textChuncker)
    {
        _db = db;
        _logger = logger;
        _kernelFactory = kernelFactory;
        _openAiSettings = openAiSettings;
        _textChuncker = textChuncker;
        _opts = options.Value;
        _extractorFactory = extractorFactory;
    }

    public async Task<VectorizationResult> IngestAsync(Stream content, string fileName, string? title = null, IReadOnlyDictionary<string, object>? metadata = null, CancellationToken ct = default)
    {
        if (content == null || !content.CanRead) throw new ArgumentException("Content stream is not readable", nameof(content));
        var extractor = _extractorFactory.GetFor(fileName);

        // Buffer the entire content so we can both extract text and persist raw bytes
        using var buffer = new MemoryStream();
        content.Position = 0;
        await content.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        // Extract text
        using var textStream = new MemoryStream(bytes, writable: false);
        var rawText = await extractor.ExtractTextAsync(textStream, ct);
        if (string.IsNullOrWhiteSpace(rawText)) throw new InvalidOperationException("No extractable text");

        // Chunk
        var chunks = _textChuncker.ChunkByApproxTokens(rawText, _opts.MaxTokensPerChunk, _opts.OverlapTokens);

        // Embed
        var kernel = _kernelFactory.Create(_openAiSettings.ChatModelId);
        var embedSvc = _kernelFactory.GetEmbeddingService(kernel);
        var embeddings = await embedSvc.GenerateEmbeddingsAsync(chunks, ct);

        // Validate dimensions
        if (embeddings.Count == 0) throw new InvalidOperationException("Embedding service returned no vectors");
        var dim = embeddings[0].Length;
        if (dim != _opts.EmbeddingDimensions)
            throw new InvalidOperationException($"Embedding dimension mismatch: got {dim}, expected {_opts.EmbeddingDimensions}. Check your embedding model vs DB schema.");

        // Persist
        var docId = Guid.NewGuid();
        var entity = new RagDocument
        {
            DocId = docId,
            Title = title ?? Path.GetFileName(fileName),
            SourcePath = fileName,
            Metadata = metadata.ToJsonDocument(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Documents.Add(entity);
        await _db.SaveChangesAsync(ct);

        // Persist original file bytes in separate table
        var stored = new StoredDocument
        {
            Id = Guid.NewGuid(),
            DocId = docId,
            FileName = Path.GetFileName(fileName),
            SizeBytes = bytes.LongLength,
            Content = bytes,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.StoredDocuments.Add(stored);
        await _db.SaveChangesAsync(ct);

        // Persist chunks with embeddings
        var toAdd = new List<RagChunk>(chunks.Count);
        for (int i = 0; i < chunks.Count; i++)
        {
            var vector = new Vector(embeddings[i].ToArray());
            toAdd.Add(new RagChunk
            {
                DocId = docId,
                ChunkIndex = i,
                Content = chunks[i],
                Embedding = vector
            });
        }
        _db.Chunks.AddRange(toAdd);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Ingested {File} as {DocId} with {Count} chunks", fileName, docId, toAdd.Count);
        return new VectorizationResult
        {
            DocumentId = docId,
            Title = entity.Title,
            ChunkCount = toAdd.Count
            
        };
    }

    public async Task<VectorizationResult> IngestFileAsync(string path, string? title = null, IReadOnlyDictionary<string, object>? metadata = null, CancellationToken ct = default)
    {
        await using var fs = File.OpenRead(path);
        return await IngestAsync(fs, path, title, metadata, ct);
    }

    public async Task<int> IngestFolderAsync(string folder, string searchPattern = "*.*", bool recurse = true, CancellationToken ct = default)
    {
        var files = Directory.EnumerateFiles(folder, searchPattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)
            .Where(f => _extractorFactory.Supports(f))
            .ToList();
        int count = 0;
        foreach (var file in files)
        {
            await using var fs = File.OpenRead(file);
            await IngestAsync(fs, file, null, null, ct);
            count++;
        }
        return count;
    }
}

internal static class MetadataExtensions
{
    public static JsonDocument? ToJsonDocument(this IReadOnlyDictionary<string, object>? dict)
    {
        if (dict == null) return null;
        var json = JsonSerializer.Serialize(dict);
        return JsonDocument.Parse(json);
    }
}

