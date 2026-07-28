using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace AdvancedRAG
{
    /// <summary>
    /// Combines Semantic (vector) and Lexical (keyword) search to find the most relevant documents.
    /// </summary>
    internal class HybridRetriever
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;
        private readonly List<Document> _database;

        public HybridRetriever(IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator, List<Document> database)
        {
            _embeddingGenerator = embeddingGenerator;
            _database = database;
        }

        public async Task<IEnumerable<string>> SearchAsync(string query, int topK = 2)
        {
            // 1. Vector Search (Semantic)
            // Use GenerateAsync to get the result, then extract the Vector property
            var embeddings = await _embeddingGenerator.GenerateAsync(new[] { query });
            var queryVector = embeddings[0].Vector;

            var vectorResults = _database
                .Select(doc => new { Doc = doc, Score = CalculateCosineSimilarity(doc.Vector.Span, queryVector.Span) })
                .OrderByDescending(x => x.Score)
                .Take(topK)
                .Select(x => x.Doc);
            
            // 2. Keyword Search (Lexical)
            // Perform a basic case-insensitive string match for the query terms
            var keywords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var keywordResults = _database
                .Where(doc => keywords.Any(k => doc.Content.Contains(k, StringComparison.OrdinalIgnoreCase)))
                .Take(topK);

            // 3. Deduplicate and Merge
            // Combine both lists and remove duplicates so we don't waste model tokens
            var combinedResults = vectorResults
                .Union(keywordResults)
                .Select(d => d.Content)
                .Distinct();

            return combinedResults;
        }

        /// <summary>
        /// Measures the mathematical similarity between two vectors. Closer to 1.0 means highly similar.
        /// </summary>
        private float CalculateCosineSimilarity(ReadOnlySpan<float> vecA, ReadOnlySpan<float> vecB)
        {
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < vecA.Length; i++)
            {
                dot += vecA[i] * vecB[i];
                normA += vecA[i] * vecA[i];
                normB += vecB[i] * vecB[i];
            }
            return normA == 0 || normB == 0 ? 0 : (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }

}
