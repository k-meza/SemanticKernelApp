namespace API.Options;

public class OpenAiSettings
{
    public string ApiKey { get; set; }
    public string ChatModelId { get; set; }
    public string EmbeddingModelId { get; set; } = "text-embedding-3-small"; // 1536 dims
}