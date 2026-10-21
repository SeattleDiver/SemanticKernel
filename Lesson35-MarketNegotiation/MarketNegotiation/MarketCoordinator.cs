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

            // Rounding each share to 2 decimal places independently ("coin rounding") can drift
            // the sum a cent or two away from totalBudget. Every share but the last is rounded
            // normally; the last bidder gets whatever remains, so the total always lands exactly
            // on budget instead of just approximately.
            var keys = bids.Keys.ToList();
            var allocation = new Dictionary<string, decimal>();
            decimal allocatedSoFar = 0m;

            for (int i = 0; i < keys.Count; i++)
            {
                if (i == keys.Count - 1)
                {
                    allocation[keys[i]] = totalBudget - allocatedSoFar;
                }
                else
                {
                    decimal share = Math.Round(bids[keys[i]].RequestedAmount * scale, 2);
                    allocation[keys[i]] = share;
                    allocatedSoFar += share;
                }
            }

            return allocation;
        }
    }
}
