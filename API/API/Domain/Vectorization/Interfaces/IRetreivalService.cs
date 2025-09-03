namespace API.Domain.Vectorization.Interfaces;

public interface IRetrievalService
{
    Task<IReadOnlyList<RetrievedChunk>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken ct = default);
}

public sealed record RetrievedChunk(
    Guid DocId,
    int ChunkIndex,
    string Content,
    float Score // smaller is closer for cosine distance
);