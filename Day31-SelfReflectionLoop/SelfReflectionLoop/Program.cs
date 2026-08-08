// Day 31: Self-Reflection Loop
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace SelfReflectionLoop
{
    /// <summary>
    /// Entry point that runs a single agent through a bounded draft/critique/revise loop.
    /// </summary>
    internal class Program
    {
        private const int MaxPasses = 3;

        /// <summary>
        /// Runs the draft/critique/revise loop against a fixed task and prints each pass.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var agent = new ReflectiveAgent(chatService);

            const string task =
                "Write EXACTLY three sentences of product description for a noise-cancelling " +
                "travel mug that keeps drinks at a set temperature for 12 hours. It must name " +
                "the temperature-lock feature explicitly and end with a call to action.";

            Console.WriteLine($"TASK: {task}\n");

            string draft = await agent.DraftAsync(task);
            Console.WriteLine($"--- Draft 1 ---\n{draft}\n");

            for (int pass = 1; pass <= MaxPasses; pass++)
            {
                SelfCritique critique = await agent.CritiqueAsync(task, draft);

                if (critique.IsSatisfactory)
                {
                    Console.WriteLine($"--- Self-critique (pass {pass}) ---\nSatisfied. Stopping.\n");
                    break;
                }

                Console.WriteLine($"--- Self-critique (pass {pass}) ---\n{critique.Feedback}\n");

                if (pass == MaxPasses)
                {
                    Console.WriteLine("Reached the revision cap; shipping the latest draft as-is.\n");
                    break;
                }

                draft = await agent.ReviseAsync(task, draft, critique.Feedback);
                Console.WriteLine($"--- Draft {pass + 1} ---\n{draft}\n");
            }

            Console.WriteLine("--- FINAL OUTPUT ---");
            Console.WriteLine(draft);
        }
    }
}
