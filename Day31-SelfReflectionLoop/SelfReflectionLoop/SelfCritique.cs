using System.Text.Json.Serialization;

namespace SelfReflectionLoop
{
    /// <summary>
    /// Structured verdict returned by <see cref="ReflectiveAgent.CritiqueAsync"/>.
    /// </summary>
    internal class SelfCritique
    {
        /// <summary>Whether the agent judged its own draft to already satisfy the task.</summary>
        [JsonPropertyName("isSatisfactory")]
        public bool IsSatisfactory { get; set; }

        /// <summary>Actionable feedback to address in the next revision; empty if satisfactory.</summary>
        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;
    }
}
