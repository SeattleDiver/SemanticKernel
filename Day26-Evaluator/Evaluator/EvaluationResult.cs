using System.Text.Json.Serialization;

namespace Evaluator
{
    internal class EvaluationResult
    {
        [JsonPropertyName("score")]
        public int Score { get; set; }

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("passed")]
        public bool Passed { get; set; }
    }
}
