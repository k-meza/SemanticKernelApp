using API.Domain.SemanticKernel.Interfaces;
using API.Options;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Domain.SemanticKernel;

public class SemanticKernelFactory : IKernelFactory
{
    private readonly ILogger<SemanticKernelFactory> _logger;
    private readonly OpenAiSettings _opts;
    

    public SemanticKernelFactory(ILogger<SemanticKernelFactory> logger, OpenAiSettings opts)
    {
        _logger = logger;
        _opts = opts;
    }

    public Kernel Create(string modelId)
    {
        var builder = Kernel.CreateBuilder();

        // OpenAI “ChatGPT”-style models
        builder.AddOpenAIChatCompletion(modelId: modelId, apiKey: _opts.ApiKey);

        // Add plugins here if needed, e.g.:
        // builder.Plugins.AddFromObject(new MathPlugin(), "math");

        return builder.Build();
    }

    public IChatCompletionService GetChatService(Kernel kernel)
        => kernel.GetRequiredService<IChatCompletionService>();
}