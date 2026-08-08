using System.Text.Json.Serialization;

namespace EpisodicMemory
{
    /// <summary>
    /// Structured result returned by <see cref="FactExtractor.ExtractAsync"/>.
    /// </summary>
    internal class FactExtractionResult
    {
        /// <summary>The fact restated concisely in third person; empty if nothing durable was said.</summary>
        [JsonPropertyName("fact")]
        public string Fact { get; set; } = string.Empty;
    }
}
