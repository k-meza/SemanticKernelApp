using System.Collections.Concurrent;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Domain.SessionManager;

public class InMemorySessionManager : ISessionManager
{
    private readonly ILogger<InMemorySessionManager> _logger;
    private readonly ConcurrentDictionary<string, ChatSession> _sessions = new();

    public InMemorySessionManager(ILogger<InMemorySessionManager> logger)
    {
        _logger = logger;
    }

    public ChatSession GetOrCreate(string connectionId, string modelId, string? systemPrompt = null)
    {
        return _sessions.GetOrAdd(connectionId, _ =>
        {
            var history = new ChatHistory();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                history.AddSystemMessage(systemPrompt!);
            else
                history.AddSystemMessage("You are a helpful assistant.");

            _logger.LogInformation("Created new chat session for ConnectionId={ConnectionId}, ModelId={ModelId}",
                connectionId, modelId);

            return new ChatSession
            {
                SessionId = connectionId, // Use connectionId as sessionId
                UserId = connectionId, // Simplified - use connectionId as userId too
                ModelId = modelId,
                History = history,
                Gate = new SemaphoreSlim(1, 1)
            };
        });
    }

    public bool TryGet(string connectionId, out ChatSession? session) =>
        _sessions.TryGetValue(connectionId, out session);

    public Task EndAsync(string connectionId)
    {
        if (_sessions.TryRemove(connectionId, out var session))
        {
            session.Gate.Dispose();
            _logger.LogInformation("Removed chat session for ConnectionId={ConnectionId}", connectionId);
        }

        return Task.CompletedTask;
    }
}