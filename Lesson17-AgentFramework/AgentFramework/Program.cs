// Day 17: Agent Framework
// ---------------------------------------------------------------------------
// The first use of Microsoft.SemanticKernel.Agents' ChatCompletionAgent - a
// higher-level abstraction over the raw IChatCompletionService + ChatHistory
// pattern used in every earlier episode. Shows that everything done manually
// so far (persona injection, history management) can be wrapped in a
// purpose-built agent object.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace AgentFramework
{
    /// <summary>Entry point that runs an interactive travel-planning chat loop driven by a <see cref="ChatCompletionAgent"/>.</summary>
    class Program
    {
        /// <summary>Builds a persona-configured travel agent and loops on user input, appending each response to shared history.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // 1. Setup the kernel
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            // 2. Create the ChatCompletionAgent
            ChatCompletionAgent travelAgent = new()
            {
                Name = "TravelAssistant",
                Instructions = "You are a world-class travel planner." +
                               "Your goal is to help users find the bets destinations based on their budget. " +
                               "Alwasy provide three options: Budget, Mid-range, and Luxury",
                Kernel = kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings {  Temperature = 0.5 })
            };

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

                try
                {
                    await foreach (var message in travelAgent.InvokeAsync(chatHistory))
                    {
                        Console.WriteLine($"\n{message.Message.AuthorName}: {message.Message.Content}");

                        // Add the agent's response to the history to maintain context
                        chatHistory.Add(message);
                    }
                }
                catch (Exception ex)
                {
                    // 5. A failed call (rate limit, network blip, bad key) would otherwise
                    // throw out of the await foreach and crash the whole console session.
                    // Report the error and let the user try again instead of losing the chat.
                    Console.WriteLine($"\n[Error] The agent could not complete this turn: {ex.Message}");
                }
            }

        }
    }
}