using System.Text.Json.Serialization;

namespace LLMFanIn
{
    /// <summary>
    /// Structured result returned by <see cref="Reducer.SynthesizeAsync"/>.
    /// </summary>
    internal class SynthesisResult
    {
        /// <summary>The single answer synthesized from all input drafts.</summary>
        [JsonPropertyName("synthesizedAnswer")]
        public string SynthesizedAnswer { get; set; } = string.Empty;

        /// <summary>What the reducer kept from each draft, one short sentence per draft.</summary>
        [JsonPropertyName("rationale")]
        public string Rationale { get; set; } = string.Empty;
    }
}
