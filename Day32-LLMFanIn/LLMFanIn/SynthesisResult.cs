using System.Text.Json.Serialization;

namespace LLMFanIn
{
    internal class SynthesisResult
    {
        [JsonPropertyName("synthesizedAnswer")]
        public string SynthesizedAnswer { get; set; } = string.Empty;

        [JsonPropertyName("rationale")]
        public string Rationale { get; set; } = string.Empty;
    }
}
