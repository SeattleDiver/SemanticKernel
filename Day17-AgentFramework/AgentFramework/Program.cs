using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace AgentFramework
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in AgentFramework.csproj) before building.");
#else
            // 1. Setup the kernel
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
#endif
            Kernel kernel = builder.Build();

            // 2. Create the ChatCompletionAgent
#if CHATGPT
            ChatCompletionAgent travelAgent = new()
            {
                Name = "TravelAssistant",
                Instructions = "You are a world-class travel planner." +
                               "Your goal is to help users find the bets destinations based on their budget. " +
                               "Alwasy provide three options: Budget, Mid-range, and Luxury",
                Kernel = kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings {  Temperature = 0.5 })
            };
#elif GOOGLE
            ChatCompletionAgent travelAgent = new()
            {
                Name = "TravelAssistant",
                Instructions = "You are a world-class travel planner." +
                               "Your goal is to help users find the bets destinations based on their budget. " +
                               "Alwasy provide three options: Budget, Mid-range, and Luxury",
                Kernel = kernel,
                Arguments = new KernelArguments(new GeminiPromptExecutionSettings {  Temperature = 0.5 })
            };
#endif

            // 3. Define the conversation
            ChatHistory chatHistory = new ChatHistory();
            Console.WriteLine("Tarvel Agent Framework Initialized");

            while(true)
            {
                Console.Write("\nUser: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // Add user input to the chat history
                chatHistory.AddUserMessage(input);

                // 4. Invoke the agent
                // The Agent Framework handles the context and generation
                Console.WriteLine("\n--- AGENT IS THINKING ---");

                await foreach (var message in travelAgent.InvokeAsync(chatHistory))
                {
                    Console.WriteLine($"\n{message.Message.AuthorName}: {message.Message.Content}");

                    // Add the agent's response to the history to maintain context
                    chatHistory.Add(message);
                }
            }
#endif
        }
    }
}