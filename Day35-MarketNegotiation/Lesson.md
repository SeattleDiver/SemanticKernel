# Day 35 — Negotiation & Market-Based Coordination

## Overview

This lesson has three teams bid for shares of a fixed shared budget across
rounds, revising their asks based on how far over budget the total is and
what everyone else is asking for - no fixed protocol decides how many
rounds it takes or who concedes. It closes a different gap than Day 34:
Day 34 resolves an open discussion with no single right answer; this
lesson resolves a genuine resource conflict, where the outcome is a
concrete allocation of money that must add up.

## Prerequisites

- **Day 34 (GroupChatDebate)** - for contrast, and the source of the
  per-turn isolation pattern reused here. Debate and negotiation both
  involve "multiple agents, no fixed turn order," but a debate has no
  scarce resource to divide and no numeric constraint that must be
  satisfied - negotiation does, and that changes what the coordinator
  actually has to do (arithmetic, not just picking a speaker).
- **Day 32 (LLMFanIn)** and **Day 33 (VotingEnsembles)** - structured JSON
  output from multiple independent participants, collected and reasoned
  about in code. This lesson's `MarketCoordinator` is plain C#, the same
  choice Day 33's `ConsensusVoter` made and for the same reason: budget
  arithmetic has one correct answer, so it belongs in code, not in another
  model call.

## Setup

- .NET 10 SDK
- A Gemini API key, available via the `GEMINI_API_KEY` environment variable
- NuGet packages:
  - `Microsoft.SemanticKernel` `1.78.0`
  - `Microsoft.SemanticKernel.Connectors.Google` `1.79.0-alpha`

```
dotnet add package Microsoft.SemanticKernel --version 1.78.0
dotnet add package Microsoft.SemanticKernel.Connectors.Google --version 1.79.0-alpha
```

## Core Concepts

**No fixed protocol means the number of rounds isn't known in advance.**
Day 18's loop runs until a literal `"APPROVED"` string appears; Day 34's
debate runs a fixed number of turns. This lesson's loop runs until the
bids simply add up to something at or under budget - which might happen on
round 1, round 4, or never, depending entirely on how the participants
respond to the pressure they're shown.

**The market signal is the only coordination mechanism.** Participants
never talk to each other directly. Each round, every participant sees the
same summary - the total ask versus the budget, and what everyone else
asked for - and independently decides whether and how much to concede.
That shared, public signal *is* the negotiation; there's no side channel
and no participant with authority to just declare an allocation.

**A well-defined tie-breaker matters as much as the negotiation itself.**
Real negotiations don't always converge on their own. `MarketCoordinator.FinalizeProportional`
is the fallback: if the round cap is reached without the total fitting,
every bid is scaled down by the same ratio so the total exactly matches
the budget. This is a real market mechanism (proportional rationing under
excess demand), not an arbitrary tie-break - and having it at all is what
keeps the loop from needing to run forever waiting for organic agreement.

**Isolating each bid matters more here than in a single-agent loop.** Each
round makes one call per participant. A single dropped call - and this
lesson's own testing hit plenty of them live, purely from provider-side
load - shouldn't force the whole negotiation to restart; it should just
mean that participant's ask carries forward unchanged into the next round,
exactly like a real bidder who didn't get a chance to revise this time.

## Full Walkthrough / Code

### `NegotiationParticipant.cs`

```csharp
namespace MarketNegotiation
{
    internal class NegotiationParticipant
    {
        public string Name { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}
```

### `Bid.cs`

```csharp
using System.Text.Json.Serialization;

namespace MarketNegotiation
{
    internal class Bid
    {
        [JsonPropertyName("requestedAmount")]
        public decimal RequestedAmount { get; set; }

        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }
}
```

### `BidNegotiator.cs`

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace MarketNegotiation
{
    internal class BidNegotiator
    {
        private readonly IChatCompletionService _chatService;

        public BidNegotiator(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<Bid> SubmitBidAsync(NegotiationParticipant participant, decimal totalBudget, string marketSignal)
        {
            string prompt = $$"""
                {{participant.Instructions}}

                The shared budget for all teams combined is ${{totalBudget}}.
                {{marketSignal}}

                Submit your bid for this round. Be reasonable - an unreasonable bid that ignores
                the budget reality will not be respected in the final allocation.

                Output ONLY valid JSON matching this schema:
                {
                    "requestedAmount": number,
                    "justification": "one short sentence, at most 20 words"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.5,
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(Bid)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<Bid>(response.Content ?? "{}") ?? Unparseable();
            }
            catch (JsonException)
            {
                return Unparseable();
            }
        }

        private static Bid Unparseable() => new()
        {
            RequestedAmount = 0,
            Justification = "Failed to parse bid response."
        };
    }
}
```

### `MarketCoordinator.cs`

```csharp
namespace MarketNegotiation
{
    internal static class MarketCoordinator
    {
        public static (bool Fits, decimal Total) CheckFit(IReadOnlyDictionary<string, Bid> bids, decimal totalBudget)
        {
            decimal total = bids.Values.Sum(b => b.RequestedAmount);
            return (total <= totalBudget, total);
        }

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
```

### `Program.cs`

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MarketNegotiation
{
    internal class Program
    {
        private const decimal TotalBudget = 10000m;
        private const int MaxRounds = 4;

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

            var lastKnownBids = participants.ToDictionary(
                p => p.Name,
                _ => new Bid { RequestedAmount = 0, Justification = "No bid received yet." });

            for (int round = 1; round <= MaxRounds; round++)
            {
                Console.WriteLine($"=== Round {round} ===");

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
```

## Explanation

The three initial needs ($5,500 + $6,000 + $4,500 = $16,000) are set well
above the $10,000 budget on purpose, guaranteeing at least one round of
real concessions rather than an immediate, unremarkable fit. Each
participant is told its own real need and told it "can operate on less" -
enough latitude to negotiate down without being told by how much, so any
concession is the model's own judgment call in response to the market
signal, not a scripted number.

`lastKnownBids` is seeded before round 1 with a `$0` placeholder per
participant specifically so a failed *first* call has something safe to
fall back to - there's no real previous bid yet on round 1, so the seed
has to be a value that can't accidentally look like a legitimate ask.

Bids are collected one participant at a time (a plain `foreach` with
`await` inside, not `Task.WhenAll`) rather than concurrently. A sealed-bid
round doesn't require true simultaneity to be a fair mechanism - what
matters is that no participant sees another's bid before submitting its
own, which holds either way, since every bid this round is generated from
the *previous* round's market signal regardless of call order.

## Expected Result

Running the program should show the total requested amount decrease round
over round as participants respond to the market signal, until either the
total fits the budget (an organic allocation) or the round cap is reached
(a proportional-rationing fallback). A clean run looks like:

```
TOTAL BUDGET: $10000

=== Round 1 ===
  Marketing: $5500 - Full funding needed for a high-impact product launch campaign.
  Engineering: $6000 - New build servers are critical for development velocity.
  DataScience: $4500 - GPU cluster upgrade required for competitive model training times.
  Total requested: $16000 (budget: $10000)

=== Round 2 ===
  Marketing: $4000 - Reduced scope campaign still covers the core launch activities.
  Engineering: $4500 - Scaled back server order while still addressing the most urgent capacity needs.
  DataScience: $3000 - A smaller GPU allocation can still meaningfully accelerate priority projects.
  Total requested: $11500 (budget: $10000)

=== Round 3 ===
  Marketing: $3500 - Further trimmed budget while preserving the launch's key channels.
  Engineering: $4000 - Minimum viable server capacity to avoid delaying the roadmap.
  DataScience: $2500 - Reduced request focused only on the highest-priority training jobs.
  Total requested: $10000 (budget: $10000)

--- ALLOCATION (bids fit the budget as requested) ---
  Marketing: $3500
  Engineering: $4000
  DataScience: $2500
```

Exact numbers and how many rounds it takes will vary between runs - that's
the point of not having a fixed protocol. If a participant's bid call
fails partway through (this lesson hit real, live provider timeouts during
its own testing), the console shows a `[WARN]` line and that participant's
previous bid simply carries forward unchanged into the total, rather than
crashing the negotiation.
