using Microsoft.Extensions.AI;

namespace EpisodicMemory
{
    /// <summary>
    /// An in-memory, vector-indexed store of episodic facts, retrievable by relevance to a
    /// query or by plain recency - kept side by side so the two strategies can be compared.
    /// </summary>
    internal class EpisodicMemoryStore
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
        private readonly List<EpisodicFact> _facts = new();

        public EpisodicMemoryStore(IEmbeddingGenerator<string, Embedding<float>> embeddingService)
        {
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// Embeds and stores a fact, tagged with the turn it was learned on.
        /// </summary>
        public async Task RememberAsync(string content, int turnNumber)
        {
            ReadOnlyMemory<float> vector = await _embeddingService.GenerateVectorAsync(content);
            _facts.Add(new EpisodicFact { Content = content, TurnNumber = turnNumber, Embedding = vector });
        }

        /// <summary>
        /// Returns the top-K stored facts ranked by cosine similarity to the query, regardless
        /// of how long ago each fact was learned.
        /// </summary>
        public async Task<IReadOnlyList<EpisodicFact>> RetrieveByRelevanceAsync(string query, int topK)
        {
            ReadOnlyMemory<float> queryVector = await _embeddingService.GenerateVectorAsync(query);
            return _facts
                .OrderByDescending(f => CosineSimilarity(f.Embedding.Span, queryVector.Span))
                .Take(topK)
                .ToList();
        }

        /// <summary>
        /// Returns the most recently learned K facts, ignoring relevance entirely.
        /// </summary>
        public IReadOnlyList<EpisodicFact> RetrieveByRecency(int topK)
        {
            return _facts.OrderByDescending(f => f.TurnNumber).Take(topK).ToList();
        }

        /// <summary>
        /// Measures how similar two embedding vectors are (1.0 = identical direction, 0.0 = unrelated).
        /// </summary>
        private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0;
            }

            return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }
}
