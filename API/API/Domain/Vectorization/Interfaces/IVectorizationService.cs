namespace API.Domain.Vectorization.Interfaces;

public interface IVectorizationService
{
    Task<VectorizationResult> IngestAsync(Stream content, string fileName, string? title = null, IReadOnlyDictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task<VectorizationResult> IngestFileAsync(string path, string? title = null, IReadOnlyDictionary<string, object>? metadata = null, CancellationToken ct = default);
    Task<int> IngestFolderAsync(string folder, string searchPattern = "*.*", bool recurse = true, CancellationToken ct = default);
}