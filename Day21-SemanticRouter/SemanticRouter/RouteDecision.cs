using System.Text.Json.Serialization;

namespace SemanticRouter
{
    /// <summary>
    /// The strict schema we force Gmini to return for routing decisions
    /// </summary>
    internal class RouteDecision
    {
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("intent")]
        public string Intent { get; set; } = string.Empty;  // e.g. TECH, BILLING, GENERAL, etc.
    }
}
