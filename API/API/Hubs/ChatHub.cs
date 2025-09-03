using System.Runtime.CompilerServices;
using System.Text;
using API.Domain.SemanticKernel.Interfaces;
using API.Domain.SessionManager;
using API.Domain.Vectorization.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace API.Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly ISessionManager _sessions;
    private readonly ISemanticKernelFactory _semanticKernelFactory;
    private readonly IRetrievalService _retrieval;

    public ChatHub(ISessionManager sessions, ISemanticKernelFactory semanticKernelFactory, ILogger<ChatHub> logger, API.Domain.Vectorization.Interfaces.IRetrievalService retrieval)
    {
        _sessions = sessions;
        _semanticKernelFactory = semanticKernelFactory;
        _logger = logger;
        _retrieval = retrieval;
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Clean up session when client disconnects
        await _sessions.EndAsync(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    // Simplified stream chat - no sessionId parameter needed
    public async IAsyncEnumerable<string> StreamChat(
        string modelId,
        string userMessage,
        double temperature = 0.2,
        int maxTokens = 800,
        string? systemPrompt = null,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        string connectionId = Context.ConnectionId;

        var session = _sessions.GetOrCreate(connectionId, modelId, systemPrompt);
        await session.Gate.WaitAsync(ct);

        try
        {
            _logger.LogInformation("StreamChat start. ConnectionId={ConnectionId}, ModelId={ModelId}",
                connectionId, modelId);

            // Build kernel and get chat service
            var kernel = _semanticKernelFactory.Create(session.ModelId);
            var chat = _semanticKernelFactory.GetChatService(kernel);

            // Add user message to history
            session.History.AddUserMessage(userMessage);

            _logger.LogInformation("Processing message. History count: {Count}", session.History.Count);

            // Retrieve relevant context via RAG (topK can be tuned or made configurable)
            var retrieved = await _retrieval.RetrieveAsync(userMessage, topK: 5, ct);
            var contextBlock = BuildContextBlock(retrieved, maxChars: 4000);

            // Create an augmented history for this turn only (do not pollute the persistent history)
            var augmented = new ChatHistory();
            foreach (var msg in session.History)
            {
                var role = msg.Role.ToString()?.ToLowerInvariant();
                if (role == "system")
                {
                    augmented.AddSystemMessage(msg.Content);
                }
                else if (role == "user")
                {
                    augmented.AddUserMessage(msg.Content);
                }
                else if (role == "assistant")
                {
                    augmented.AddAssistantMessage(msg.Content);
                }
            }
            // Inject retrieved context as a system message for the current turn
            if (!string.IsNullOrWhiteSpace(contextBlock))
            {
                augmented.AddSystemMessage("The following context may be relevant to the user's last question. Use it to answer accurately. If the answer isn't in the context, say you don't know.\n\n" + contextBlock);
            }

            // Create execution settings
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            // Stream response
            var assistantResponse = new StringBuilder();
            await foreach (var chunk in
                           chat.GetStreamingChatMessageContentsAsync(augmented, settings, kernel, ct))
            {
                var token = chunk.Content;
                if (!string.IsNullOrEmpty(token))
                {
                    assistantResponse.Append(token);
                    yield return token;
                }
            }

            // Add assistant response to history
            var fullResponse = assistantResponse.ToString();
            session.History.AddAssistantMessage(fullResponse);

            _logger.LogInformation("StreamChat completed. ConnectionId={ConnectionId}, Response length: {Length}",
                connectionId, fullResponse.Length);
        }
        finally
        {
            session.Gate.Release();
        }
    }

    // Optional: Method to get chat history count
    public int GetHistoryCount()
    {
        string connectionId = Context.ConnectionId;
        if (_sessions.TryGet(connectionId, out var session))
        {
            return session.History.Count;
        }

        return 0;
    }

    // Optional: Method to clear chat history
    public async Task ClearHistory()
    {
        string connectionId = Context.ConnectionId;
        await _sessions.EndAsync(connectionId);
        await Clients.Caller.SendAsync("HistoryCleared");
    }

    private static string BuildContextBlock(IReadOnlyList<RetrievedChunk> chunks, int maxChars)
    {
        if (chunks == null || chunks.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        foreach (var c in chunks.OrderBy(x => x.Score))
        {
            var piece = $"[DocId: {c.DocId}, Chunk: {c.ChunkIndex}, Score: {c.Score:F4}]\n{c.Content}\n---\n";
            if (sb.Length + piece.Length > maxChars) break;
            sb.Append(piece);
        }
        return sb.ToString();
    }
}