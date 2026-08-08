namespace VotingEnsembles
{
    /// <summary>
    /// Structured result returned by <see cref="ConsensusVoter.Tally"/>.
    /// </summary>
    internal class ConsensusResult
    {
        /// <summary>The answer with the most votes.</summary>
        public string WinningAnswer { get; init; } = string.Empty;

        /// <summary>Whether two or more answers were tied for the most votes.</summary>
        public bool IsTie { get; init; }

        /// <summary>Vote count per normalized answer.</summary>
        public IReadOnlyDictionary<string, int> VoteCounts { get; init; } = new Dictionary<string, int>();
    }
}
