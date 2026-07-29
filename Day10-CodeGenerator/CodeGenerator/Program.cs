// Day 10: The Code Generator
// ---------------------------------------------------------------------------
// A persona-driven "developer agent" that generates and iteratively refines
// C# code across a multi-turn conversation. Low Temperature/TopP suppress
// creative variance in the generated code, and the conversation history lets
// the agent "remember" and revise its own earlier output on request. This
// episode's output becomes the code the Critic (Day 11) reviews.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace CodeGenerator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Setup kernel with Gemini Flash 2.5
            var builder = Kernel.CreateBuilder();
            var apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
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

                var settings = new GeminiPromptExecutionSettings
                {
                    Temperature = 0.2,
                    TopP = 0.1
                };

                var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
                if (response.Content != null)
                {
                    Console.WriteLine(response.Content);

                    // Add to history so the agent can "refactor" or "debug" in the next turn.
                    chatHistory.AddAssistantMessage(response.Content);
                }
            }
        }
    }
}