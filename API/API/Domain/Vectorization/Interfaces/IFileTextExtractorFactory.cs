namespace API.Domain.Vectorization.Interfaces;

public interface IFileTextExtractorFactory
{
    IFileTextExtractor GetFor(string fileNameOrPath);
    bool Supports(string fileNameOrPath);
}
