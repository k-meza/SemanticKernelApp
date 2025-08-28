using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace API.Domain.SemanticKernel.Interfaces;

public interface ISemanticKernelFactory
{
    Kernel Create(string modelId);
    IChatCompletionService GetChatService(Kernel kernel);
}
