using API.Domain.Vectorization.Interfaces;

namespace API.Domain.Vectorization.TextExctractor;

public sealed class FileTextExtractorFactory : IFileTextExtractorFactory
{
    private readonly IReadOnlyList<IFileTextExtractor> _extractors;

    public FileTextExtractorFactory(IEnumerable<IFileTextExtractor> extractors)
    {
        _extractors = extractors?.ToList() ?? throw new ArgumentNullException(nameof(extractors));
    }

    public IFileTextExtractor GetFor(string fileNameOrPath)
    {
        if (string.IsNullOrWhiteSpace(fileNameOrPath))
            throw new ArgumentException("File name or path is required", nameof(fileNameOrPath));

        return _extractors.FirstOrDefault(e => e.CanHandle(fileNameOrPath))
               ?? throw new NotSupportedException($"No extractor registered for file: {fileNameOrPath}");
    }

    public bool Supports(string fileNameOrPath)
        => _extractors.Any(e => e.CanHandle(fileNameOrPath));
}