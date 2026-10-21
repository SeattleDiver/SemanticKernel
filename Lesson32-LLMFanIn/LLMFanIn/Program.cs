// Day 32: LLM-Based Fan-In
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    /// <summary>
    /// Entry point that fans out N independent drafts and fans them back in via an LLM reducer.
    /// </summary>
    internal class Program
    {
        private const int FanOutCount = 3;

        /// <summary>
        /// Generates <see cref="FanOutCount"/> concurrent drafts and synthesizes them into one answer.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var drafter = new Drafter(chatService);
            var reducer = new Reducer(chatService);

            const string task =
                "Explain what a closure is to someone who has never programmed before, in one " +
                "paragraph, using a single real-world analogy.";

            Console.WriteLine($"TASK: {task}\n");

            var draftTasks = Enumerable.Range(0, FanOutCount)
                .Select(_ => drafter.GenerateDraftAsync(task));
            string[] drafts = await Task.WhenAll(draftTasks);

            for (int i = 0; i < drafts.Length; i++)
            {
                Console.WriteLine($"--- Draft {i + 1} ---\n{drafts[i]}\n");
            }

            SynthesisResult synthesis = await reducer.SynthesizeAsync(task, drafts);

            Console.WriteLine($"--- Synthesis Rationale ---\n{synthesis.Rationale}\n");
            Console.WriteLine("--- FINAL OUTPUT ---");
            Console.WriteLine(synthesis.SynthesizedAnswer);
        }
    }
}
