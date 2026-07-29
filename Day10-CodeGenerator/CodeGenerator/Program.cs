using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace CodeGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in CodeGenerator.csproj) before building.");
#else
            // 1. Setup kernel with a chat model
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            var modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            var modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
#endif
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. Define the developer persona
            var chatHistory = new ChatHistory(
                "You are an expert C# developer agent. " +
                "Your goal is to write clean, maintainable, and high-performance C# code. " +
                "Rules: " +
                "1. Always user modern .NET 8 syntax (file-scoped namespaces, primary constructors where applicable). " +
                "2. Include XML documentation comments for all public members. " +
                "3. Ensure the code is self-contained and includes necessary using directives. " +
                "4. Output only the code within markdown blocks.  Not conversational filter."
            );

            Console.WriteLine("Developer agent ready.  Describe the class or function you need.");

            while (true)
            {
                Console.Write("\nRequest: ");
                string? userRequest = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userRequest)) continue;
                if (userRequest.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // 3. Add the request to history
                chatHistory.AddUserMessage($"Write a c# implemenation for: {userRequest}");
                Console.WriteLine("\n --- Generating code ---\n");

#if CHATGPT
                var settings = new OpenAIPromptExecutionSettings
                {
                    Temperature = 0.2,
                    TopP = 0.1
                };
#elif GOOGLE
                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0.2,
                    TopP = 0.1
                };
#endif

                var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
                if (response.Content != null)
                {
                    Console.WriteLine(response.Content);

                    // Add to history so the agent can "refactor" or "debug" in the next turn.
                    chatHistory.AddAssistantMessage(response.Content);
                }
            }
#endif
        }
    }
}