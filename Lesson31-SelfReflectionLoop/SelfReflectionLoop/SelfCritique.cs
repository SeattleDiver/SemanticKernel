using System.Text.Json.Serialization;

namespace SelfReflectionLoop
{
    /// <summary>
    /// Structured verdict returned by <see cref="ReflectiveAgent.CritiqueAsync"/>. A single
    /// field carries the verdict: an empty <see cref="Feedback"/> means the draft already
    /// satisfies the task. A separate boolean field was tried first and reliably came back
    /// missing from the model's response - see this lesson's Explanation section.
    /// </summary>
    internal class SelfCritique
    {
        /// <summary>Actionable feedback to address in the next revision; empty if satisfactory.</summary>
        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;

        /// <summary>Whether the agent judged its own draft to already satisfy the task.</summary>
        /// <remarks>
        /// Excluded from serialization via <see cref="JsonIgnoreAttribute"/>. Without this, SK's
        /// JSON-schema generation for a typed <c>ResponseFormat</c> picks up this computed
        /// property via reflection and marks it required in OpenAI's strict structured-output
        /// schema - forcing the model to also emit an undocumented "IsSatisfactory" field on
        /// every call, which defeats the single-field design this class is built around.
        /// </remarks>
        [JsonIgnore]
        public bool IsSatisfactory => string.IsNullOrWhiteSpace(Feedback);
    }
}
