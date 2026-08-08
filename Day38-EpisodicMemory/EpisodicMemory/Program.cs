// Day 38: Vector-Indexed Episodic Memory
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EpisodicMemory
{
    /// <summary>
    /// Entry point that records a short conversation into episodic memory, then compares
    /// recency-based retrieval against relevance-based retrieval for a later query.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Extracts durable facts from a scripted conversation, then retrieves for a query
        /// whose most relevant fact is also the oldest one stored.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            builder.AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var extractor = new FactExtractor(chatService);
            var memory = new EpisodicMemoryStore(embeddingService);

            string[] turns =
            {
                "My dog Biscuit is allergic to chicken, so I have to check every treat label.",
                "I'm planning a trip to Japan next spring.",
                "My favorite color is teal.",
                "I just started a new job at a marine biology lab.",
                "I like hiking on weekends when the weather is nice.",
            };

            Console.WriteLine("=== Recording conversation turns ===");
            for (int i = 0; i < turns.Length; i++)
            {
                int turnNumber = i + 1;
                string? fact = await extractor.ExtractAsync(turns[i]);

                if (fact is not null)
                {
                    await memory.RememberAsync(fact, turnNumber);
                    Console.WriteLine($"  Turn {turnNumber}: remembered - \"{fact}\"");
                }
                else
                {
                    Console.WriteLine($"  Turn {turnNumber}: nothing durable to remember.");
                }
            }

            const string query = "What treats should I avoid buying for my pet?";
            Console.WriteLine($"\n=== New query (turn {turns.Length + 1}) ===\n{query}\n");

            IReadOnlyList<EpisodicFact> byRecency = memory.RetrieveByRecency(2);
            Console.WriteLine("--- Retrieval by RECENCY (last 2 facts learned) ---");
            foreach (var fact in byRecency)
            {
                Console.WriteLine($"  [Turn {fact.TurnNumber}] {fact.Content}");
            }

            IReadOnlyList<EpisodicFact> byRelevance = await memory.RetrieveByRelevanceAsync(query, 2);
            Console.WriteLine("\n--- Retrieval by RELEVANCE (top 2 by cosine similarity to the query) ---");
            foreach (var fact in byRelevance)
            {
                Console.WriteLine($"  [Turn {fact.TurnNumber}] {fact.Content}");
            }
        }
    }
}
