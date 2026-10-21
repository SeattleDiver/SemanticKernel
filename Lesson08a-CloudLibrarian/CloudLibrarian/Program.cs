// Day 8a: The Cloud Librarian
// ---------------------------------------------------------------------------
// Day 8's RAG pipeline, minus the hand-rolled parts. Instead of embedding a
// knowledge base into an in-memory list and computing cosine similarity by
// hand, this episode registers the exact same five facts into a real cloud
// vector database (Pinecone) through Semantic Kernel's IVectorStore
// abstraction, and lets Pinecone's own index do the similarity search. Same
// grounded-answer result as Day 8, but the retrieval step is now something
// that scales past a handful of documents living in a List<T>.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Pinecone;
using Pinecone;

namespace CloudLibrarian
{
    // The record shape Pinecone will store. The three attributes are what
    // Semantic Kernel's Vector Store abstraction (Microsoft.Extensions.VectorData)
    // reads to map this POCO onto the underlying Pinecone index - a key field,
    // one or more filterable/returnable data fields, and the embedding itself.
    // 1536 matches text-embedding-3-small's default output size.
    /// <summary>The record shape stored in Pinecone: an id, the source text, and its embedding vector.</summary>
    public class KnowledgeRecord
    {
        /// <summary>Unique identifier for this record within the collection.</summary>
        [VectorStoreKey]
        public string Id { get; set; } = "";

        /// <summary>The source fact's plain-text content, returned alongside search results.</summary>
        [VectorStoreData]
        public string Text { get; set; } = "";

        /// <summary>The embedding vector for <see cref="Text"/>, indexed by Pinecone for cosine-similarity search.</summary>
        [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineSimilarity)]
        public ReadOnlyMemory<float> Vector { get; set; }
    }

    /// <summary>Entry point that runs Day 8's RAG pipeline against a real Pinecone vector store instead of an in-memory list.</summary>
    class Program
    {
        /// <summary>Upserts the knowledge base into Pinecone, retrieves the best match for a question, and answers using only that context.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 1: Read both API keys. OpenAI generates the embeddings and
            // answers the question; Pinecone stores and searches the vectors.
            string openAiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            string pineconeApiKey = Environment.GetEnvironmentVariable("PINECONE_API_KEY")
                ?? throw new Exception("PINECONE_API_KEY environment variable is not set.");

            // Step 2: Initialize the Kernel with chat and embedding models, same
            // as Day 8.
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", openAiApiKey);
            builder.AddOpenAIEmbeddingGenerator("text-embedding-3-small", openAiApiKey);
            Kernel kernel = builder.Build();

            var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            // Step 3: Connect to Pinecone and get (or create) the collection
            // that will hold our knowledge base. EnsureCollectionExistsAsync is
            // idempotent - safe to call every run, it only creates the index the
            // first time.
            var pineconeClient = new PineconeClient(pineconeApiKey);
            var vectorStore = new PineconeVectorStore(pineconeClient);
            var collection = vectorStore.GetCollection<string, KnowledgeRecord>("company-knowledge-base");

            try
            {
                await collection.EnsureCollectionExistsAsync();
            }
            catch (Exception ex)
            {
                // A missing/invalid Pinecone API key, an unreachable project, or a
                // plan limit would otherwise surface as a raw HTTP exception with
                // no indication of what to check first.
                Console.WriteLine($"[ERROR] Could not reach Pinecone: {ex.Message}");
                Console.WriteLine("Verify PINECONE_API_KEY and that your Pinecone project allows creating a serverless index.");
                return;
            }

            // Step 4: The same "company knowledge base" from Day 8, so the two
            // lessons are directly comparable.
            string[] rawDocuments = new[]
            {
                "The company WiFi password is 'guest2026!'. It changes every Monday.",
                "Employees are allowed 3 weeks of paid time off per year, which rolls over.",
                "The office building is closed on federal holidays, and keycard access is disabled.",
                "For IT support issues, please submit a ticket to the help desk at helpdesk@company.com. Do not call the IT desk directly.",
                "The microwave in the 2nd-floor breakroom is strictly for reheating food. Do not use it to cook raw food."
            };

            Console.WriteLine("Embedding and upserting the knowledge base into Pinecone...");

            for (int i = 0; i < rawDocuments.Length; i++)
            {
                var vector = await embeddingService.GenerateVectorAsync(rawDocuments[i]);
                await collection.UpsertAsync(new KnowledgeRecord
                {
                    Id = i.ToString(),
                    Text = rawDocuments[i],
                    Vector = vector
                });
            }

            // Step 5: Embed the user's question the same way, then let Pinecone's
            // own index find the closest match instead of scanning a List<T> by
            // hand the way Day 8 did.
            string userQuestion = "Where can I heat my lunch?";
            Console.WriteLine($"User Question: '{userQuestion}'");

            var questionVector = await embeddingService.GenerateVectorAsync(userQuestion);

            KnowledgeRecord? bestMatch = null;
            await foreach (var result in collection.SearchAsync(questionVector, top: 1))
            {
                bestMatch = result.Record;
                Console.WriteLine($"[CLOUD RETRIEVAL] Found relevant document (score {result.Score:F3}): {bestMatch.Text}");
            }

            // Guard against an empty index (e.g. the upsert loop above failed
            // silently) the same way Day 8 guards against an empty in-memory list.
            if (bestMatch is null)
            {
                Console.WriteLine("No matching documents were found in the Pinecone index.");
                return;
            }

            // Step 6: Same grounded prompt template as Day 8.
            string promptTemplate = @"
You are a helpful company librarian. Answer the user's question using ONLY the provided context.
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

            var answer = await kernel.InvokePromptAsync(promptTemplate, arguments);

            Console.WriteLine("--- AI LIBRARIAN ANSWER ---");
            Console.WriteLine(answer.ToString().Trim());
            Console.WriteLine("---------------------------");
        }
    }
}
