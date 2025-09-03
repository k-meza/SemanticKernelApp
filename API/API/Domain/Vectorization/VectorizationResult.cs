namespace API.Domain.Vectorization;

public record VectorizationResult()
{
    public Guid DocumentId { get; set; }
    public string Title { get; set; }
    public int ChunkCount { get; set; }
}