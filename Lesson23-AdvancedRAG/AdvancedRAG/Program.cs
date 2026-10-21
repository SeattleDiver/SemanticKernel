// Day 23: Advanced RAG (Hybrid Search)
// ---------------------------------------------------------------------------
// Upgrades the naive vector-only RAG from Day 8 to Hybrid Search
// (HybridRetriever.cs): vector similarity for semantic meaning plus a
// keyword pass for exact terms (part numbers, IDs) that embeddings alone
// can miss. Uses Microsoft.Extensions.AI's standard IEmbeddingGenerator
// interface rather than a proprietary SK type, so the embedding backend can
// be swapped without touching HybridRetriever.cs or Document.cs at all.
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace AdvancedRAG
{
    /// <summary>Entry point that seeds a mock knowledge base and answers one question through a hybrid-search RAG pipeline.</summary>
    internal class Program
    {
        /// <summary>Vectorizes a mock knowledge base, then retrieves and answers a hardcoded question using hybrid search.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            // Chat model for reasoning, Embedding model for vectorization
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            builder.AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey);

            Kernel kernel = builder.Build();

            // Extract the .NET standard embedding interface from the built Kernel
            var embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            // 2. Seed the mock database
            Console.WriteLine("📚 Vectorizing Knowledge Base...");
            var rawData = new[] {
                "The Alpha-99 server rack requires a 220V power supply.",
                "Standard cloud servers operate at 110V.",
                "Routine maintenance for the Alpha-99 should occur every 90 days."
            };

            var database = new List<Document>();
            foreach(var text in rawData)
            {
                // Generate vectors using text-embedding-3-small
                var embedding = await embeddingGenerator.GenerateAsync(text);
                Document doc = new Document
                {
                    Content = text,
                    Vector = embedding.Vector
                };
                database.Add(doc);
            }

            // 3. Initialize cleanly decoupled components
            HybridRetriever retriever = new HybridRetriever(embeddingGenerator, database);
            RagAgent ragAgent = new RagAgent(kernel, retriever);

            // 4. Test the Hybrid RAG workflow
            string query = "What is the maintenance schedule for the Alpha-99";
            // Step 4a: "\n" for a blank line before the query, not "\b" (backspace) - easy escape-sequence typo to miss
            Console.WriteLine($"\nUser Query: {query}");

            string response = await ragAgent.AnswerAsync(query);
            Console.WriteLine($"\nAI Answer: {response}");
        }
    }
}
