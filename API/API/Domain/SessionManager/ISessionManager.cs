namespace API.Domain.SessionManager;

public interface ISessionManager
{
    ChatSession GetOrCreate(string connectionId, string modelId, string? systemPrompt = null);
    bool TryGet(string connectionId, out ChatSession? session);
    Task EndAsync(string connectionId);
}
