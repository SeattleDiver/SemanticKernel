// Day 1: The Conversationalist
// ---------------------------------------------------------------------------
// The first project in the series - a continuous, terminal-based chatbot.
// Demonstrates the core Semantic Kernel building blocks every later episode
// builds on: Kernel.CreateBuilder(), a chat completion connector,
// IChatCompletionService, and a ChatHistory object that gives the model
// memory of the conversation across turns.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Conversationalist
{
    /// <summary>Entry point hosting a continuous, terminal-based chatbot backed by Semantic Kernel and OpenAI.</summary>
    internal class Program
    {
        /// <summary>Builds the kernel, then runs the read-eval-print chat loop until the user types "exit".</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 1: Setup a kernel builder - the factory used to configure
            // and construct the Kernel (the app's central AI service registry)
            var builder = Kernel.CreateBuilder();

            // Step 2: Add a chat completion service to the builder
            // Fetch the API Key from the environment variable and add the service to the builder
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            string modelId = "gpt-4.1-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Step 3: Build the kernel
            Kernel kernel = builder.Build();

            // Step 4: Retrieve the chat completion service from the kernel
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Step 5: Initialize chat history with a system prompt
            var chatHistory = new ChatHistory("You are a helpful, friendly, and concise AI Assistant.");
            Console.WriteLine("Chatbot initialized.  Type 'exit' to quit");

            // Step 6: The conversation loop - reads a line from the console,
            // hands it to the model along with the running history, prints
            // the reply, and repeats until the user types "exit"
            while(true)
            {
                Console.Write("\nUser: ");
                string? userInput = Console.ReadLine();

                // A null read means stdin hit EOF (e.g. piped input) - there will never be
                // another line, so looping on "continue" here would spin forever.
                if (userInput is null)
                {
                    break;
                }

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

                // Step 7: Ask the AI to generate a response based on the chat history and the latest user input.
                // The call is wrapped in a try/catch because a network blip, an invalid/expired
                // API key, or a rate-limit response would otherwise throw here and crash the
                // whole chat session, losing the conversation instead of just this one turn.
                ChatMessageContent response;
                try
                {
                    response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sorry, something went wrong talking to the AI: {ex.Message}");
                    continue;
                }

                Console.WriteLine($"AI response: {response.Content}");

                // This step is not optional: if the assistant's reply is never
                // added back to the history, the model has no memory of its
                // own previous answers and the conversation loses context.
                if (response.Content != null)
                {
                    chatHistory.AddAssistantMessage(response.Content);
                }
            }
        }
    }
}
