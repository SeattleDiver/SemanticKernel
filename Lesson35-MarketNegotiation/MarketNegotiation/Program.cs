// Day 35: Negotiation & Market-Based Coordination
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarketNegotiation
{
    /// <summary>
    /// Entry point that runs a multi-round budget negotiation among three competing teams.
    /// </summary>
    internal class Program
    {
        private const decimal TotalBudget = 10000m;
        private const int MaxRounds = 4;

        /// <summary>
        /// Runs bidding rounds until every ask fits the budget or the round cap is hit, then
        /// finalizes an allocation.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var negotiator = new BidNegotiator(chatService);

            var participants = new List<NegotiationParticipant>
            {
                new()
                {
                    Name = "Marketing",
                    Instructions =
                        "You represent the Marketing team, negotiating for a share of a shared " +
                        "quarterly budget. You need funding for a product launch campaign - " +
                        "roughly $5,500 would fully cover it, but you can operate on less.",
                },
                new()
                {
                    Name = "Engineering",
                    Instructions =
                        "You represent the Engineering team, negotiating for a share of a shared " +
                        "quarterly budget. You need funding for new build servers - roughly " +
                        "$6,000 would fully cover it, but you can operate on less.",
                },
                new()
                {
                    Name = "DataScience",
                    Instructions =
                        "You represent the Data Science team, negotiating for a share of a shared " +
                        "quarterly budget. You need funding for a GPU cluster upgrade - roughly " +
                        "$4,500 would fully cover it, but you can operate on less.",
                },
            };

            Console.WriteLine($"TOTAL BUDGET: ${TotalBudget}\n");

            string marketSignal = "This is the first round; no bids have been submitted yet.";

            // Only bids that were actually received and parsed are remembered - there is no $0
            // placeholder seed. A team whose call fails (or whose reply can't be parsed) before it
            // has ever bid validly has nothing to carry forward, so that round can't settle.
            var lastValidBids = new Dictionary<string, Bid>();

            for (int round = 1; round <= MaxRounds; round++)
            {
                Console.WriteLine($"=== Round {round} ===");

                // Bids are collected one at a time rather than concurrently - a sealed-bid
                // round doesn't require simultaneity, and each call is still isolated: a
                // single dropped connection (or unparseable reply) shouldn't crash the round,
                // just carry that participant's last valid bid forward instead.
                var bidsByName = new Dictionary<string, Bid>();
                foreach (var p in participants)
                {
                    Bid bid;
                    try
                    {
                        bid = await negotiator.SubmitBidAsync(p, TotalBudget, marketSignal);
                    }
                    catch (Exception ex)
                    {
                        if (!lastValidBids.TryGetValue(p.Name, out Bid? previous))
                        {
                            Console.WriteLine($"  [WARN] {p.Name} has no valid bid this round and none to carry forward: {ex.Message}");
                            continue;
                        }

                        Console.WriteLine($"  [WARN] {p.Name}'s bid failed, reusing their last valid bid: {ex.Message}");
                        bid = previous;
                    }

                    bidsByName[p.Name] = bid;
                    lastValidBids[p.Name] = bid;
                    Console.WriteLine($"  {p.Name}: ${bid.RequestedAmount} - {bid.Justification}");
                }

                // A team without a valid bid can't be part of a settlement: the round only
                // settles when every team has a valid bid AND the total fits the budget.
                bool everyTeamBid = bidsByName.Count == participants.Count;
                (bool fits, decimal total) = MarketCoordinator.CheckFit(bidsByName, TotalBudget);
                Console.WriteLine($"  Total requested: ${total} (budget: ${TotalBudget})\n");

                if (fits && everyTeamBid)
                {
                    Console.WriteLine("--- ALLOCATION (bids fit the budget as requested) ---");
                    foreach (var (name, bid) in bidsByName)
                    {
                        Console.WriteLine($"  {name}: ${bid.RequestedAmount}");
                    }
                    return;
                }

                if (round == MaxRounds)
                {
                    Console.WriteLine("--- No organic consensus within the round cap. Applying proportional rationing. ---");
                    var finalAllocation = MarketCoordinator.FinalizeProportional(bidsByName, TotalBudget);
                    Console.WriteLine("--- FINAL ALLOCATION ---");
                    foreach (var (name, amount) in finalAllocation)
                    {
                        Console.WriteLine($"  {name}: ${amount}");
                    }
                    foreach (var p in participants.Where(p => !bidsByName.ContainsKey(p.Name)))
                    {
                        Console.WriteLine($"  {p.Name}: no valid bid received - not allocated");
                    }
                    return;
                }

                string asksSummary = string.Join(", ", bidsByName.Select(kv => $"{kv.Key}: ${kv.Value.RequestedAmount}"));
                marketSignal = fits
                    ? $"Round {round} results: not every team submitted a valid bid, so the round could not " +
                      $"settle. Current asks were [{asksSummary}], totaling ${total} against the ${TotalBudget} " +
                      "budget. Submit your bid again."
                    : $"Round {round} results: the total requested (${total}) exceeds the ${TotalBudget} " +
                      $"budget by ${total - TotalBudget}. Current asks were [{asksSummary}]. You must revise " +
                      "your ask down to help the total fit the budget - holding firm will not be respected.";
            }
        }
    }
}
