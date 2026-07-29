#define GOOGLE
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace Conversationalist
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in Conversationalist.csproj) before building.");
#else
            // Step 1: Setup a kernel builder
            var builder = Kernel.CreateBuilder();

            // Step 2: Add a chat completion service to the builder
            // Fetch the API Key from the environment variable and add the service to the builder
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash-lite";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
#endif

            // Step 3: Build the kernel
            Kernel kernel = builder.Build();

            // Step 4: Retrieve the chat completion service from the kernel
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Step 5: Initialize chat history with a system prompt
            var chatHistory = new ChatHistory("You are a helpful, friendly, and concise AI Assistant.");
            Console.WriteLine("Chatbot initialized.  Type 'exit' to quit");

            while(true)
            {
                Console.Write("\nUser: ");
                string? userInput = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting the chat. Goodbye!");
                    break;
                }

                chatHistory.AddUserMessage(userInput);

                // Ask the AI to generate a response based on the chat history and the latest user input
                var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: kernel);

                Console.WriteLine($"AI response: {response.Content}");

                if (response.Content != null)
                {
                    chatHistory.AddAssistantMessage(response.Content);
                }
            }
#endif
        }
    }
}
