using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace BasicRagAgent
{
    public class KnowledgeDocument
    {
        public string Text { get; set; }
        public ReadOnlyMemory<float> Vector { get; set; }   
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 2: Initialize the Kernel with Both Chat and Embedding models
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            // Add the chat model and embedding model to the kernel
            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
            builder.AddGoogleAIEmbeddingGenerator("gemini-embedding-001", apiKey);

            Kernel kernel = builder.Build();

            // Step 3: Extract the Embedding service from the kernel
            var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            // Step 4: Create our "Company knowledge base" as a list of KnowledgeDocument objects
            string[] rawDocuments = new[]
            {
                "The company WiFi password is 'guest2026!'.  It changes every Monday.",
                "Employees are allowed 3 weeks of paid time off per year, which rolls over.",
                "The office building is closed on federal holidays, and keycad access is disabled.",
                "For IT support issues, please submit a ticket to the help desk at helpdesk@company.com.  Do not call the IT desk directly",
                "The microwave in the 2nd-floor breakroom is strictly for reheating food.  Do not use it to cook raw food."
            };

            Console.WriteLine("Generating embeddings for the knowledge base documents...");

            var knowledgeBase = new List<KnowledgeDocument>();
            foreach (var text in rawDocuments)
            {
                // Convert each text document into an embedding vector
                var vector = await embeddingService.GenerateVectorAsync(text);
                knowledgeBase.Add(new KnowledgeDocument {  Text = text, Vector = vector });
            }

            // Step 5: The user's questions
            //string userQuestion = "My computer screen is frozen.  How do I get someone to fix it?";
            string userQuestion = "Where can I heat my lunch?";
            Console.WriteLine($"User Question: '{userQuestion}'");

            // Step 6: Generate an embedding for the user's question
            var questionVector = await embeddingService.GenerateVectorAsync(userQuestion);

            // Step 7: Search for the most relevant document using cosine similarity
            var bestMatch = knowledgeBase
                .OrderByDescending(doc => CalculateCosineSimilarity(doc.Vector.Span, questionVector.Span))
                .FirstOrDefault();

            Console.WriteLine("$[RAG RETRIEVAL] Found relevant document: {bestMatch?.Text}");

            // Step 8: Build the RAG prompt
            string promptTemplate = @"
You are a helpful company librarian.  Answer the user's question using ONLY the provide context.
If the context does not contain the answer, say 'I don't have enough information to answer that question.'  
Do not make up an answer.
Do not use outside knowledge.  
Do not provide any information that is not in the context.

CONTEXT:
{{$context}}

USER QUESTION:
{{$question}}
";

            var arguments = new KernelArguments
            {
                { "context", bestMatch.Text },
                { "question", userQuestion }
            };

            Console.WriteLine("Agent is reading the retrieved document and generating an answer...");

            // Step 9: Execute the prompt
            var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

            // Step 10: Display the answer
            Console.WriteLine("--- AI LIBRARIAN ANSWER ---");
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("---------------------------");
        }

        static float CalculateCosineSimilarity(ReadOnlySpan<float> vectorA, ReadOnlySpan<float> vectorB)
        {
            float dotProduct = 0, normA = 0, normB = 0;
            for(int i = 0; i < vectorA.Length; i++) 
            {
                dotProduct += vectorA[i] * vectorB[i];
                normA += vectorA[i] * vectorA[i];
                normB += vectorB[i] * vectorB[i];
            }
            // Protect against division by zero
            if (normA == 0 || normB == 0) return 0;
            return (float)(dotProduct / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }
}