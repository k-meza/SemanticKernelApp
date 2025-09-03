namespace API.Domain.Vectorization.Interfaces;

public interface ISkEmbeddingGenerator
{
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> inputs, CancellationToken ct = default);
}