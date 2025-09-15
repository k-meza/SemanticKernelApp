using API.Domain.SemanticKernel.Interfaces;
using API.Domain.SemanticKernel.Plugins;
using API.Domain.Vectorization;
using API.Domain.Vectorization.Interfaces;
using API.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Domain.SemanticKernel;

public sealed class SemanticKernelFactory : ISemanticKernelFactory
{
    private readonly ILogger<SemanticKernelFactory> _logger;
    private readonly IServiceProvider _services;
    private readonly OpenAiSettings _opts;
    private readonly VectorizationOptions _vectorizationOptions;

    public SemanticKernelFactory(ILogger<SemanticKernelFactory> logger, IOptions<OpenAiSettings> opts,
        VectorizationOptions vectorizationOptions, IServiceProvider services)
    {
        _logger = logger;
        _opts = opts.Value;
        _vectorizationOptions = vectorizationOptions;
        _services = services;
    }

    public Kernel Create(string modelId)
    {
        var builder = Kernel.CreateBuilder();

        // Chat (SK)
        builder.AddOpenAIChatCompletion(
            modelId: string.IsNullOrWhiteSpace(modelId) ? _opts.ChatModelId : modelId,
            apiKey: _opts.ApiKey);

        // Embeddings (ME.AI via SK extension)
        // Suppress experimental API warning for AddOpenAIEmbeddingGenerator
        #pragma warning disable SKEXP0010
        builder.AddOpenAIEmbeddingGenerator(_opts.EmbeddingModelId, _opts.ApiKey,
            dimensions: _vectorizationOptions.EmbeddingDimensions);
        #pragma warning restore SKEXP0010

        //REGISTER PLUGINS HERE
        RegisterPlugins(builder);

        return builder.Build();
    }

    public IChatCompletionService GetChatService(Kernel kernel)
    {
        return kernel.GetRequiredService<IChatCompletionService>();
    }
        

    public ISkEmbeddingGenerator GetEmbeddingService(Kernel kernel)
    {
        // Resolve Microsoft.Extensions.AI generator and adapt it
        var inner = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
        return new SkEmbeddingGenerator(inner);
    }

    private void RegisterPlugins(IKernelBuilder kernelBuilder)
    {
        kernelBuilder.Plugins.AddFromObject(_services.GetRequiredService<TerminalPlugin>(), "terminal");
    }

}