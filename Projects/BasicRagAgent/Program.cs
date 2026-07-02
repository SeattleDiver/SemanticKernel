using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using Google.GenAI;

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

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
            builder.AddGoogleAITextEmbeddingGeneration("text-embedding-004", apiKey);

        }
    }
}