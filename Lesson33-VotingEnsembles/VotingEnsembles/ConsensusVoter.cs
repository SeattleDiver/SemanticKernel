namespace VotingEnsembles
{
    /// <summary>
    /// Deterministic, code-only fan-in: tallies votes across samples instead of synthesizing them.
    /// </summary>
    internal static class ConsensusVoter
    {
        /// <summary>
        /// Strips formatting noise (e.g. a leading "$") so answers that agree in substance
        /// group into the same vote instead of fragmenting into separate ones.
        /// </summary>
        public static string Normalize(string answer) => answer.Trim().TrimStart('$').Trim();

        /// <summary>
        /// Groups the given samples by normalized answer and returns the majority result.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if <paramref name="samples"/> is empty; there is no majority to compute.</exception>
        public static ConsensusResult Tally(IReadOnlyList<ReasoningSample> samples)
        {
            if (samples.Count == 0)
            {
                throw new ArgumentException("Cannot tally votes over an empty sample set.", nameof(samples));
            }

            var counts = samples
                .GroupBy(s => Normalize(s.FinalAnswer), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count());

            int maxVotes = counts.Values.Max();
            var winners = counts.Where(kv => kv.Value == maxVotes).Select(kv => kv.Key).ToList();

            return new ConsensusResult
            {
                WinningAnswer = winners[0],
                IsTie = winners.Count > 1,
                VoteCounts = counts
            };
        }
    }
}
