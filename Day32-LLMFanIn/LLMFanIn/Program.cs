using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LLMFanIn
{
    internal class Program
    {
        private const int FanOutCount = 3;

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
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
