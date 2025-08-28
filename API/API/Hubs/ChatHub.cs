using System.Runtime.CompilerServices;
using System.Text;
using API.Domain.SemanticKernel.Interfaces;
using API.Domain.SessionManager;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace API.Hubs;

public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly ISessionManager _sessions;
    private readonly ISemanticKernelFactory _semanticKernelFactory;

    public ChatHub(ISessionManager sessions, ISemanticKernelFactory semanticKernelFactory, ILogger<ChatHub> logger)
    {
        _sessions = sessions;
        _semanticKernelFactory = semanticKernelFactory;
        _logger = logger;
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

            // Create execution settings
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = temperature,
                MaxTokens = maxTokens
            };

            // Stream response
            var assistantResponse = new StringBuilder();
            await foreach (var chunk in
                           chat.GetStreamingChatMessageContentsAsync(session.History, settings, kernel, ct))
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
}