using System;
using Microsoft.Extensions.VectorData;

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
}
