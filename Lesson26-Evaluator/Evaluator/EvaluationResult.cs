using System.Text.Json.Serialization;

namespace Evaluator
{
    /// <summary>The judge's strict-JSON verdict on a target agent's response: a numeric score, its reasoning, and a pass/fail flag.</summary>
    internal class EvaluationResult
    {
        /// <summary>The judge's numeric score from 1 (inaccurate) to 5 (perfectly accurate).</summary>
        [JsonPropertyName("score")]
        public int Score { get; set; }

        /// <summary>The judge's step-by-step reasoning for the score.</summary>
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        /// <summary>Whether the response passed (true when the score is 4 or 5).</summary>
        [JsonPropertyName("passed")]
        public bool Passed { get; set; }
    }
}
