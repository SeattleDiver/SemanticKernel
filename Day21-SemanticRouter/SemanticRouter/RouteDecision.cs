// RouteDecision
// ---------------------------------------------------------------------------
// Plain data-transfer object mapped from the router's forced-JSON response
// via JsonSerializer. No provider-specific code here - just the shape of the
// decision every part of the app agrees on.
using System.Text.Json.Serialization;

namespace SemanticRouter
{
    /// <summary>
    /// The strict schema we force the model to return for routing decisions.
    /// </summary>
    internal class RouteDecision
    {
        /// <summary>The router's brief explanation for why it chose this category.</summary>
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>The classified intent: TECH, BILLING, or GENERAL.</summary>
        [JsonPropertyName("intent")]
        public string Intent { get; set; } = string.Empty;  // e.g. TECH, BILLING, GENERAL, etc.
    }
}
