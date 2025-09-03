namespace API.Domain.Vectorization.Interfaces;

public interface ITextChuncker
{
    List<string> ChunkByApproxTokens(string text, int maxTokens, int overlapTokens);
}