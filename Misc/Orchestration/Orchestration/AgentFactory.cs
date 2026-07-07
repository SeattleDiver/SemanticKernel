using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

public static class AgentFactory
{
    public static ChatCompletionAgent CreateAgent(string name, string instructions, Kernel kernel)
    {
        return new ChatCompletionAgent
        {
            Name = name,
            Instructions = instructions,
            Kernel = kernel
        };
    }
}