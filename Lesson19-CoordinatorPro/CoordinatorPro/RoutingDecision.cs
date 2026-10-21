using System.Text.Json.Serialization;

namespace CoordinatorPro
{
    // The strict JSON schema the Coordinator must respond with on every turn.
    /// <summary>The Coordinator's strict-JSON routing decision: its reasoning and which agent should act next.</summary>
    public class RoutingDecision
    {
        /// <summary>The Coordinator's step-by-step reasoning for this routing decision.</summary>
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = "";

        /// <summary>Which agent should act next: "CODER", "AUDITOR", or "COMPLETE".</summary>
        [JsonPropertyName("nextAgent")]
        public string NextAgent { get; set; } = "";
    }
}
