namespace MarketNegotiation
{
    /// <summary>
    /// Deterministic, code-only market logic: checks whether bids fit the budget, and if
    /// negotiation never converges, applies a proportional-rationing fallback allocation.
    /// </summary>
    internal static class MarketCoordinator
    {
        /// <summary>
        /// Returns whether the sum of the given bids fits within the total budget, along with
        /// that sum.
        /// </summary>
        public static (bool Fits, decimal Total) CheckFit(IReadOnlyDictionary<string, Bid> bids, decimal totalBudget)
        {
            decimal total = bids.Values.Sum(b => b.RequestedAmount);
            return (total <= totalBudget, total);
        }

        /// <summary>
        /// Scales every bid down proportionally so the total exactly fits the budget - the
        /// standard market mechanism for rationing when demand exceeds supply and negotiation
        /// hasn't organically converged within the allowed number of rounds.
        /// </summary>
        public static IReadOnlyDictionary<string, decimal> FinalizeProportional(
            IReadOnlyDictionary<string, Bid> bids, decimal totalBudget)
        {
            decimal total = bids.Values.Sum(b => b.RequestedAmount);
            if (total <= 0)
            {
                return bids.Keys.ToDictionary(name => name, _ => 0m);
            }

            decimal scale = totalBudget / total;
            return bids.ToDictionary(kv => kv.Key, kv => Math.Round(kv.Value.RequestedAmount * scale, 2));
        }
    }
}
