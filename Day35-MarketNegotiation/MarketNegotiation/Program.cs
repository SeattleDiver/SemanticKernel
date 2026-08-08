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
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
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

            // Seeded with a zero bid per participant so a call that fails on round 1 (before
            // there's any prior bid to fall back to) still has something safe to reuse.
            var lastKnownBids = participants.ToDictionary(
                p => p.Name,
                _ => new Bid { RequestedAmount = 0, Justification = "No bid received yet." });

            for (int round = 1; round <= MaxRounds; round++)
            {
                Console.WriteLine($"=== Round {round} ===");

                // Bids are collected one at a time rather than concurrently - a sealed-bid
                // round doesn't require simultaneity, and each call is still isolated: a
                // single dropped connection shouldn't crash the round, just carry that
                // participant's last known bid forward instead.
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
                        Console.WriteLine($"  [WARN] {p.Name}'s bid call failed, reusing their last known bid: {ex.Message}");
                        bid = lastKnownBids[p.Name];
                    }

                    bidsByName[p.Name] = bid;
                    lastKnownBids[p.Name] = bid;
                    Console.WriteLine($"  {p.Name}: ${bid.RequestedAmount} - {bid.Justification}");
                }

                (bool fits, decimal total) = MarketCoordinator.CheckFit(bidsByName, TotalBudget);
                Console.WriteLine($"  Total requested: ${total} (budget: ${TotalBudget})\n");

                if (fits)
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
                    return;
                }

                string asksSummary = string.Join(", ", bidsByName.Select(kv => $"{kv.Key}: ${kv.Value.RequestedAmount}"));
                marketSignal =
                    $"Round {round} results: the total requested (${total}) exceeds the ${TotalBudget} " +
                    $"budget by ${total - TotalBudget}. Current asks were [{asksSummary}]. You must revise " +
                    "your ask down to help the total fit the budget - holding firm will not be respected.";
            }
        }
    }
}
