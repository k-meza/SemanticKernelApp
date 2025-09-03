using API.Domain.Vectorization.Interfaces;
using Microsoft.Extensions.AI;

namespace API.Domain.Vectorization;

internal sealed class SkEmbeddingGenerator : ISkEmbeddingGenerator
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _inner;
    
    public SkEmbeddingGenerator(IEmbeddingGenerator<string, Embedding<float>> inner) => _inner = inner;

    public async Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(IList<string> inputs,
        CancellationToken ct = default)
    {
        // Microsoft.Extensions.AI expects IEnumerable<string>; returns IReadOnlyList<Embedding<float>>
        var embeddings = await _inner.GenerateAsync(inputs, cancellationToken: ct);
        return embeddings.Select(e => e.Vector).ToList(); // normalize to IReadOnlyList<ReadOnlyMemory<float>>
    }
}