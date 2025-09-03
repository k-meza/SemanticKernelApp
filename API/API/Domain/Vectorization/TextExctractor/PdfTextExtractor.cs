using System.Text;
using System.Text.RegularExpressions;
using API.Domain.Vectorization.Interfaces;
using UglyToad.PdfPig;

namespace API.Domain.Vectorization.TextExctractor;

public class PdfTextExtractor : IFileTextExtractor
{
    public bool CanHandle(string path) => path.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);


    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;


        var sb = new StringBuilder();
        using var doc = PdfDocument.Open(ms);
        foreach (var page in doc.GetPages())
        {
            foreach (var word in page.GetWords())
            {
                sb.Append(word.Text);
                sb.Append(' ');
            }
            sb.AppendLine();
        }
        var text = Regex.Replace(sb.ToString(), "\n{3,}", "\n\n");
        text = Regex.Replace(text, "[ \t]{2,}", " ");
        return text.Trim();
    }
}