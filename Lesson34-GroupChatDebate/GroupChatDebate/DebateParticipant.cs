namespace GroupChatDebate
{
    /// <summary>
    /// One debate persona: an identity, a moderator-facing description, and its system instructions.
    /// </summary>
    internal class DebateParticipant
    {
        /// <summary>The participant's display name and unique identifier within the debate.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>A one-line description shown to the moderator when choosing who speaks next.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>The persona instructions used when generating this participant's turn.</summary>
        public string Instructions { get; set; } = string.Empty;
    }
}
