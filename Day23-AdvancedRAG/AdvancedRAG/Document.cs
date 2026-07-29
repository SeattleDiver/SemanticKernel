// Document
// ---------------------------------------------------------------------------
// A single knowledge-base entry: raw text plus its embedding vector,
// searched by both HybridRetriever's vector and keyword passes.
namespace AdvancedRAG
{
    /// <summary>
    /// Represents a stored document containing both raw text and its mathematical vector representation.
    /// </summary>
    internal class Document
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Content { get; set; } = string.Empty;
        public ReadOnlyMemory<float> Vector { get; set; }
    }
}
