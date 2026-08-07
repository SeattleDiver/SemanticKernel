using System.Text.Json.Serialization;

namespace SelfReflectionLoop
{
    internal class SelfCritique
    {
        [JsonPropertyName("isSatisfactory")]
        public bool IsSatisfactory { get; set; }

        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;
    }
}
