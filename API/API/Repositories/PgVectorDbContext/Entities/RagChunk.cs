using Pgvector;

namespace API.Repositories.PgVectorDbContext.Entities;

public class RagChunk
{
    public long ChunkId { get; set; }
    public Guid DocId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;


    // Pgvector vector column
    public Vector Embedding { get; set; } = null!;


    public RagDocument Document { get; set; } = null!;
}