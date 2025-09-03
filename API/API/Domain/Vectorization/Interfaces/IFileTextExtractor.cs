namespace API.Domain.Vectorization.Interfaces;

public interface IFileTextExtractor
{
    bool CanHandle(string fileNameOrPath);
    Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default);
}