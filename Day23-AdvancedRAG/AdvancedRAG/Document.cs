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
        /// <summary>A unique identifier for this document, generated when the instance is created.</summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>The document's raw text content.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>The embedding vector for <see cref="Content"/>, used for vector similarity search.</summary>
        public ReadOnlyMemory<float> Vector { get; set; }
    }
}
