using System.Text.Json.Serialization;

namespace VotingEnsembles
{
    /// <summary>
    /// Structured result returned by <see cref="ReasoningSampler.SampleAsync"/>.
    /// </summary>
    internal class ReasoningSample
    {
        // finalAnswer is declared first (and requested first in the prompt) so it lands
        // early in the model's output. A model's "step by step" reasoning can otherwise run
        // long enough to exhaust the response before finalAnswer ever gets written - this
        // was found against Gemini, but the same defensive field ordering is kept regardless
        // of provider since it's cheap insurance against any model's tendency to ramble.
        /// <summary>The sample's final answer, as a short exact value.</summary>
        [JsonPropertyName("finalAnswer")]
        public string FinalAnswer { get; set; } = string.Empty;

        /// <summary>A brief explanation of how this sample arrived at its answer.</summary>
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;
    }
}
