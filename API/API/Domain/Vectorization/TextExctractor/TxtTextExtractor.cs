using System.Text;
using API.Domain.Vectorization.Interfaces;

namespace API.Domain.Vectorization.TextExctractor;

public class TxtTextExtractor : IFileTextExtractor
{
    public bool CanHandle(string path) => path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".md", StringComparison.OrdinalIgnoreCase);
    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var text = await reader.ReadToEndAsync();
        return text.Trim();
    }
}