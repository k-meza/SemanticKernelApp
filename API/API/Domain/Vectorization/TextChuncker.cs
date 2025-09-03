using System.Text;
using System.Text.RegularExpressions;
using API.Domain.Vectorization.Interfaces;

namespace API.Domain.Vectorization;

public class TextChunker : ITextChuncker
{
    public List<string> ChunkByApproxTokens(string text, int maxTokens, int overlapTokens)
    {
        if (text is null) throw new ArgumentNullException(nameof(text));
        if (maxTokens <= 0) return new List<string>(0);

        var maxChars     = Math.Max(100, maxTokens * 4);
        var overlapChars = Math.Clamp(overlapTokens * 4, 0, maxChars - 1);
        var stride       = Math.Max(1, maxChars - overlapChars);  // <-- fixed

        // NOTE: Regex.Split materializes all paragraphs. For huge texts consider a streaming splitter,
        // but keeping it here for minimal changes.
        var paragraphs = Regex.Split(text, "\r?\n\r?\n")
                              .Where(p => !string.IsNullOrWhiteSpace(p))
                              .ToArray();
        if (paragraphs.Length == 0) paragraphs = new[] { text };

        // Pre-size list to reduce resizes
        var approxCount = Math.Max(1, text.Length / stride) + 2;
        var chunks = new List<string>(approxCount);

        var current = new StringBuilder(Math.Min(text.Length, maxChars));

        foreach (var para in paragraphs)
        {
            var p = para.AsSpan().Trim(); // span trim = no new string
            if (p.Length == 0) continue;

            // Try to append the whole paragraph to the current chunk
            if (current.Length + p.Length + 1 <= maxChars)
            {
                if (current.Length > 0) current.AppendLine();
                current.Append(p);
                continue;
            }

            // Flush current if it has content
            if (current.Length > 0)
            {
                TrimTrailingNewlines(current);
                chunks.Add(current.ToString());
                current.Clear();
            }

            // Paragraph fits alone
            if (p.Length <= maxChars)
            {
                current.Append(p);
                continue;
            }

            // Slide a fixed-size window over the paragraph
            var start = 0;
            while (start < p.Length)
            {
                var len = Math.Min(maxChars, p.Length - start);
                chunks.Add(new string(p.Slice(start, len)));
                if (start + len >= p.Length) break; // reached end
                start += stride;                     // <-- use fixed stride
            }
        }

        if (current.Length > 0)
        {
            TrimTrailingNewlines(current);
            chunks.Add(current.ToString());
        }

        return chunks;
    }

    private static void TrimTrailingNewlines(StringBuilder sb)
    {
        // Avoid ToString().Trim() double allocation
        while (sb.Length > 0 && (sb[^1] == '\n' || sb[^1] == '\r'))
            sb.Length--;
    }
}
