using System.Text.Json.Serialization;

namespace GroupChatDebate
{
    /// <summary>
    /// Structured result returned by <see cref="DebateModerator.SelectNextSpeakerAsync"/>.
    /// </summary>
    internal class SpeakerDecision
    {
        /// <summary>The exact name of the participant chosen to speak next.</summary>
        [JsonPropertyName("nextSpeaker")]
        public string NextSpeaker { get; set; } = string.Empty;

        /// <summary>A short explanation of why this participant was chosen.</summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
