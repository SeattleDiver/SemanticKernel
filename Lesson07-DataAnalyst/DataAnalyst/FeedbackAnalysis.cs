using System.Text.Json.Serialization;

namespace DataAnalyst
{
    // The fixed schema every extraction must match. JsonPropertyName keeps the
    // wire format lowerCamelCase (what we ask the model for in the prompt)
    // while the C# side stays PascalCase.
    /// <summary>Fixed shape every feedback extraction must match: sentiment, product, a one-line summary, and a priority score.</summary>
    public class FeedbackAnalysis
    {
        /// <summary>The feedback's overall sentiment: "Positive", "Neutral", or "Negative".</summary>
        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; } = "Unknown";

        /// <summary>The product or feature area the feedback discusses, or "Unknown" if unclear.</summary>
        [JsonPropertyName("product")]
        public string Product { get; set; } = "Unknown";

        /// <summary>A one-sentence, neutral summary of the feedback.</summary>
        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        /// <summary>An urgency score from 1 (no action needed) to 5 (urgent, needs immediate attention).</summary>
        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }
}
