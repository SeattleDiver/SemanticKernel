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
}
