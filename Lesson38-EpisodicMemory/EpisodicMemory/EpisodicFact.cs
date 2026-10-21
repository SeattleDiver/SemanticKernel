namespace EpisodicMemory
{
    /// <summary>
    /// One remembered fact, tagged with the turn it was learned on and its embedding vector.
    /// </summary>
    internal class EpisodicFact
    {
        /// <summary>The fact, restated concisely in third person.</summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>Which conversation turn this fact was extracted from.</summary>
        public int TurnNumber { get; set; }

        /// <summary>The embedding vector used for relevance-based retrieval.</summary>
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
