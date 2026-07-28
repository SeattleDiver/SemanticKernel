using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

// AddOpenAIEmbeddingGenerator is still marked experimental by the Semantic Kernel team.
#pragma warning disable SKEXP0010

namespace AdvancedRAG
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";
            string embeddingModelId = Environment.GetEnvironmentVariable("OPENAI_EMBEDDING_MODEL") ?? "text-embedding-3-small";

            // Chat model for reeasoning, Embedding model for vectorization
            builder.AddOpenAIChatCompletion(modelId, apiKey);
            builder.AddOpenAIEmbeddingGenerator(embeddingModelId, apiKey);

            Kernel kernel = builder.Build();

            // Exgtract the .NET standard embedding interface from the built Kernel
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
                // Generate vectors using text-embedding-004
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
            Console.WriteLine($"\bUser Query: {query}");

            string response = await ragAgent.AnswerAsync(query);
            Console.WriteLine($"\nAI Answer: {response}");
        }
    }
}
