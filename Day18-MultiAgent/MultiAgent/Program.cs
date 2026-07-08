using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace MultiAgent
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY key is missing");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            Kernel kernel = builder.Build();

            // 1. Define the copywriter agent
            ChatCompletionAgent copywriter = new ChatCompletionAgent()
            {
                Name = "Copywriter",
                Instructions = "You write catchy 5-word marketing slogans.  Generate one and wait for feedback.",
                Kernel = kernel
            };

            ChatCompletionAgent editor = new ChatCompletionAgent()
            {
                Name = "Editor",
                Instructions = "You review slogans.  If they are good, say 'APPROVED'.  If not, suggest one improvement.",
                Kernel = kernel
            };

            #pragma warning disable SKEXP0110
            AgentGroupChat chat = new AgentGroupChat()
            {
            };
        }
    }
}