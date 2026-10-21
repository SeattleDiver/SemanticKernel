// Day 8: Basic RAG Agent
// ---------------------------------------------------------------------------
// A naive Retrieval-Augmented Generation pipeline built from first principles
// (no vector database): embed a small knowledge base, embed the user's
// question, rank documents by hand-rolled cosine similarity, and stuff the
// single best match into a grounded prompt. Seeing this done manually here
// makes it clear what a real vector store automates in later episodes.
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;

namespace RagAgent
{
    // A single knowledge-base entry: the original text plus its embedding
    // vector, so similarity can be computed without re-embedding on every query.
    /// <summary>A single knowledge-base entry: the source text plus its precomputed embedding vector.</summary>
    public class KnowledgeDocument
    {
        /// <summary>The document's original plain-text content.</summary>
        public string Text { get; set; }

        /// <summary>The embedding vector for <see cref="Text"/>, used to compute similarity without re-embedding on every query.</summary>
        public ReadOnlyMemory<float> Vector { get; set; }
    }

    /// <summary>Entry point that runs a hand-rolled RAG pipeline: embed, retrieve by cosine similarity, then answer grounded on the match.</summary>
    class Program
    {
        /// <summary>Embeds a small knowledge base, retrieves the best match for a question, and answers using only that context.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 2: Initialize the Kernel with Both Chat and Embedding models
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            string modelId = "gpt-4.1-mini";

            // Add the chat model and embedding model to the kernel
            builder.AddOpenAIChatCompletion(modelId, apiKey);
            builder.AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey);

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

            Console.WriteLine($"[RAG RETRIEVAL] Found relevant document: {bestMatch?.Text}");

            // Step 7b: Guard against an empty/missing knowledge base. Without this,
            // a null bestMatch would crash a few lines below with a raw
            // NullReferenceException instead of a clear, friendly message.
            if (bestMatch is null)
            {
                Console.WriteLine("No knowledge base documents are available to answer this question.");
                return;
            }

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

        // Measures how "similar" two embedding vectors are (1.0 = identical
        // direction/meaning, 0.0 = unrelated). This is the core math behind
        // semantic search: documents whose vectors point in a similar
        // direction to the question's vector are considered relevant.
        /// <summary>Computes the cosine similarity between two embedding vectors.</summary>
        /// <param name="vectorA">The first embedding vector.</param>
        /// <param name="vectorB">The second embedding vector.</param>
        /// <returns>A value from -1.0 to 1.0, where 1.0 means the vectors point in the same direction; 0.0 if either vector has zero magnitude.</returns>
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