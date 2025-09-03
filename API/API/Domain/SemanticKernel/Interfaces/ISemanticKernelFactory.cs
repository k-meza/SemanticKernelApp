using API.Domain.Vectorization.Interfaces;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace API.Domain.SemanticKernel.Interfaces;

public interface ISemanticKernelFactory
{
    Kernel Create(string modelId);
    IChatCompletionService GetChatService(Kernel kernel);
    ISkEmbeddingGenerator GetEmbeddingService(Kernel kernel);
}
