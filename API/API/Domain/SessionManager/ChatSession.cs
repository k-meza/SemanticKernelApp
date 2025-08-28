using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Domain.SessionManager;

public record ChatSession()
{
    public string SessionId { get; set; }
    public string UserId { get; set; }
    public string ModelId { get; set; }
    public ChatHistory History { get; set; }
    public SemaphoreSlim Gate { get; set; }
}