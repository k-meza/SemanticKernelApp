using System.Text.Json;

namespace API.Repositories.PgVectorDbContext.Entities;

public class RagDocument
{
    public Guid DocId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public JsonDocument? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;


    public ICollection<RagChunk> Chunks { get; set; } = new List<RagChunk>();
}