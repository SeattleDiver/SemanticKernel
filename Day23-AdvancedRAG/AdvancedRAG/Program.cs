using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace AdvancedRAG
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            // Chat model for reeasoning, Embedding model for vectorization
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            builder.AddGoogleAIEmbeddingGenerator("gemini-embedding-001", apiKey);

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
