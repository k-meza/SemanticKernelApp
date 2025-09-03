namespace API.Options;

public class VectorizationOptions
{
// Chunking (token-approx by chars; ~4 chars/token heuristic)
    public int MaxTokensPerChunk { get; set; } = 800;
    public int OverlapTokens { get; set; } = 100;


// Safety: expected embedding dimensionality for pgvector column
    public int EmbeddingDimensions { get; set; } = 1536; // text-embedding-3-small
}