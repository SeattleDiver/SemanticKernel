namespace MarketNegotiation
{
    /// <summary>
    /// One team competing for a share of a fixed shared budget.
    /// </summary>
    internal class NegotiationParticipant
    {
        /// <summary>The participant's display name and unique identifier in the negotiation.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Persona instructions describing what this participant needs funding for.</summary>
        public string Instructions { get; set; } = string.Empty;
    }
}
