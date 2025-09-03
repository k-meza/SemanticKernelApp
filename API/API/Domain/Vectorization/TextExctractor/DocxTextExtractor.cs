using System.Text;
using System.Text.RegularExpressions;
using API.Domain.Vectorization.Interfaces;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace API.Domain.Vectorization.TextExctractor;

public class DocxTextExtractor : IFileTextExtractor
{
    public bool CanHandle(string path) => path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase);


    public async Task<string> ExtractTextAsync(Stream content, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        ms.Position = 0;


        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(ms, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        if (body != null)
        {
            foreach (var para in body.Descendants<Paragraph>())
            {
                sb.AppendLine(para.InnerText);
            }
        }
        var text = Regex.Replace(sb.ToString(), "\n{3,}", "\n\n");
        return text.Trim();
    }
}