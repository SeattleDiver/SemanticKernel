// ---------------------------------------------------------------------------
// Day 25: Long-Term Memory
//
// Gives an agent persistent, cross-session memory of facts about the user
// (see MemoryManager.cs/UserMemory.cs: facts are extracted from conversation
// and saved to user_memory.json so they survive a restart). This entry point
// wires up the Kernel, the MemoryManager (fact extraction/persistence), and
// the MemoryAgent (chat), then runs the conversational loop that ties them
// together.
// ---------------------------------------------------------------------------


using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace LongTermMemory
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. Initialize Components
            var memoryManager = new MemoryManager(chatService);
            var memoryAgent = new MemoryAgent(chatService);

            Console.WriteLine("Long-Term memory agent online");
            Console.WriteLine($"Loaded {memoryManager.CurrentMemory.Facts.Count} existing facts.");
            Console.WriteLine("(Type 'exit' to quit)\n");

            // 3. The Conversational Loop
            while(true)
            {
                Console.Write("User: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // Step A: The Agent responds using the currently loaded memory
                string response = await memoryAgent.ChatAsync(input, memoryManager.CurrentMemory);
                Console.WriteLine($"\nAI {response}");

                // Step B: Fire-and-forget extraction in the background
                // We use await here to ensure it finishes before the next loop, 
                // but structurally this operates as a background processor.
                await memoryManager.ExtractAndSaveFactAsync(input);
                Console.WriteLine();
            }
        }
    }
}
