# Day 33 — Voting & Self-Consistency Ensembles

## Overview

This lesson samples the same question multiple times and aggregates the
independent answers by majority vote, instead of synthesizing them into a
new one. It's built on Day 32's fan-in mechanics (concurrent sampling,
then combining), but for a different class of problem: one with a single
correct answer, where the goal is to let the crowd cancel out one sample's
mistake rather than blend everyone's phrasing together.

## Prerequisites

- **Day 32 (LLM Fan-In)** — the fan-out half of this lesson (`Task.WhenAll`
  over N independent calls) is identical. What changes is the fan-in half:
  Day 32's `Reducer` is an LLM call that *synthesizes* free-text drafts;
  this lesson's `ConsensusVoter` is plain C# that *tallies* discrete
  answers. Seeing both back to back is the point — same fan-out, two
  legitimately different fan-in strategies.
- **Day 7 (DataAnalyst)** and **Day 31 (Self-Reflection Loop)** — structured
  JSON output via `ResponseFormat`, and per-item defensive error handling
  that keeps one bad response from taking down a whole batch.

## Setup

- .NET 10 SDK
- An OpenAI API key, available via the `OPENAI_API_KEY` environment variable
- NuGet packages:
  - `Microsoft.SemanticKernel` `1.78.0`
  - `Microsoft.SemanticKernel.Connectors.OpenAI` `1.78.0`

```
dotnet add package Microsoft.SemanticKernel --version 1.78.0
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI --version 1.78.0
```

## Core Concepts

**Self-consistency.** For a multi-step reasoning problem, a single sampled
answer can take a wrong turn partway through and still sound confident.
Self-consistency samples several independent reasoning paths for the exact
same question and takes whichever final answer shows up most often. A path
that stumbles is just one vote among several that didn't.

**Synthesis vs. voting — pick by whether there's a right answer.** Day 32's
task ("explain a closure with an analogy") has no single correct output, so
blending the best parts of several drafts produces something strictly
better. This lesson's task ("how much does the ball cost?") has exactly one
correct answer, so blending doesn't make sense — averaging "$0.05" and
"$0.10" isn't a more-correct answer, it's a nonsensical one. When outputs
are discrete and checkable, voting is the right aggregation; when they're
open-ended prose, synthesis is.

**The vote itself is plain code, on purpose.** `ConsensusVoter.Tally` is a
`GroupBy`/`Max` over a `Dictionary`, not another model call. Asking an LLM
to "pick the winner" here would reintroduce exactly the single-point-of-
failure risk the vote exists to cancel out — and would cost an extra call
for something a dictionary already does exactly right.

**Formatting noise fragments votes.** Two samples that both mean "5 cents"
might write `"0.05"` and `"$0.05"`. Left alone, that's two answers with one
vote each instead of one answer with two votes. `ConsensusVoter.Normalize`
strips the noise (a leading `$`, surrounding whitespace) before grouping,
so votes consolidate on meaning, not on incidental formatting.

**An ensemble should survive losing a member.** Voting doesn't need all N
samples to agree — it needs enough of them to. `Program.cs` isolates each
sample's call in its own `try`/`catch`; one dropped connection out of five
just costs one vote, not the whole run. A malformed JSON response is
excluded the same way, even though it doesn't throw: a sample that comes
back as the sentinel `"UNPARSEABLE"` is filtered out before tallying, so
it can't accidentally cast (or win) a vote for a value nobody actually said.

## Full Walkthrough / Code

### `ReasoningSample.cs`

The structured shape each independent sample returns. `finalAnswer` is
declared — and requested in the prompt — before `reasoning`, which matters
more here than it looks:

```csharp
using System.Text.Json.Serialization;

namespace VotingEnsembles
{
    internal class ReasoningSample
    {
        // finalAnswer is declared first (and requested first in the prompt) so it lands
        // early in the model's output. A model's "step by step" reasoning can otherwise run
        // long enough to exhaust the response before finalAnswer ever gets written - this
        // was found against Gemini, but the same defensive field ordering is kept regardless
        // of provider since it's cheap insurance against any model's tendency to ramble.
        [JsonPropertyName("finalAnswer")]
        public string FinalAnswer { get; set; } = string.Empty;

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;
    }
}
```

### `ReasoningSampler.cs`

One independent, high-temperature attempt at the problem:

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace VotingEnsembles
{
    internal class ReasoningSampler
    {
        private readonly IChatCompletionService _chatService;

        public ReasoningSampler(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<ReasoningSample> SampleAsync(string question)
        {
            string prompt = $$"""
                Solve the problem below. Work through it step by step internally, then report
                your answer - many people get this kind of problem wrong by skipping the steps.

                PROBLEM:
                {{question}}

                Output ONLY valid JSON matching this schema:
                {
                    "finalAnswer": "The final answer only, as a short exact value (e.g. 0.05), with no extra words.",
                    "reasoning": "Your reasoning, in at most 3 concise sentences."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.8,
                ResponseFormat = typeof(ReasoningSample)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<ReasoningSample>(response.Content ?? "{}")
                    ?? Unparseable();
            }
            catch (JsonException)
            {
                return Unparseable();
            }
        }

        private static ReasoningSample Unparseable() => new()
        {
            Reasoning = "Failed to parse this sample's JSON response.",
            FinalAnswer = "UNPARSEABLE"
        };
    }
}
```

### `ConsensusResult.cs` and `ConsensusVoter.cs`

The deterministic fan-in. No model call anywhere in this file:

```csharp
namespace VotingEnsembles
{
    internal class ConsensusResult
    {
        public string WinningAnswer { get; init; } = string.Empty;

        public bool IsTie { get; init; }

        public IReadOnlyDictionary<string, int> VoteCounts { get; init; } = new Dictionary<string, int>();
    }
}
```

```csharp
namespace VotingEnsembles
{
    internal static class ConsensusVoter
    {
        // Different samples that agree on the answer can still disagree on
        // formatting ("$0.05" vs "0.05"). Votes are tallied on this normalized
        // form so formatting noise doesn't fragment an otherwise-unanimous answer.
        public static string Normalize(string answer) => answer.Trim().TrimStart('$').Trim();

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
```

### `Program.cs`

Fan out N isolated samples, then tally:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace VotingEnsembles
{
    internal class Program
    {
        private const int SampleCount = 5;

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var sampler = new ReasoningSampler(chatService);

            const string question =
                "A bat and a ball cost $1.10 in total. The bat costs $1.00 more than the ball. " +
                "How much does the ball cost?";

            Console.WriteLine($"PROBLEM: {question}\n");

            // Each sample is isolated: a single dropped connection shouldn't crash a vote
            // that four other samples completed fine - it should just lose one vote.
            var sampleTasks = Enumerable.Range(0, SampleCount).Select(async i =>
            {
                try
                {
                    return await sampler.SampleAsync(question);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Sample {i + 1} failed and was excluded from voting: {ex.Message}");
                    return null;
                }
            });
            ReasoningSample?[] rawSamples = await Task.WhenAll(sampleTasks);
            ReasoningSample[] samples = rawSamples.Where(s => s is not null).Select(s => s!).ToArray();

            if (samples.Length == 0)
            {
                Console.WriteLine("\nAll samples failed - no consensus can be computed.");
                return;
            }

            for (int i = 0; i < samples.Length; i++)
            {
                Console.WriteLine($"--- Sample {i + 1} (answer: {samples[i].FinalAnswer}) ---\n{samples[i].Reasoning}\n");
            }

            // A parse failure isn't a dropped connection - it's a returned-but-unusable sample.
            // Exclude it the same way, so a malformed response can't cast a real vote for the
            // literal string "UNPARSEABLE".
            ReasoningSample[] votableSamples = samples.Where(s => s.FinalAnswer != "UNPARSEABLE").ToArray();
            int excludedCount = samples.Length - votableSamples.Length;
            if (excludedCount > 0)
            {
                Console.WriteLine($"[WARN] {excludedCount} sample(s) returned unparseable JSON and were excluded from voting.\n");
            }

            if (votableSamples.Length == 0)
            {
                Console.WriteLine("\nAll samples were unparseable - no consensus can be computed.");
                return;
            }

            if (votableSamples.Length < SampleCount)
            {
                Console.WriteLine($"Tally is based on {votableSamples.Length} of {SampleCount} requested samples.\n");
            }

            ConsensusResult consensus = ConsensusVoter.Tally(votableSamples);

            Console.WriteLine("--- Vote Tally ---");
            foreach (var (answer, votes) in consensus.VoteCounts.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"  {answer}: {votes} vote(s)");
            }

            if (consensus.IsTie)
            {
                Console.WriteLine($"\nTIE - no single answer received a majority of {votableSamples.Length} samples.");
                Console.WriteLine("\n--- NO CONSENSUS (showing one tied answer) ---");
            }
            else
            {
                Console.WriteLine("\n--- CONSENSUS ANSWER ---");
            }

            Console.WriteLine(consensus.WinningAnswer);
        }
    }
}
```

## Explanation

The problem — "a bat and a ball cost $1.10 total, the bat costs $1.00 more
than the ball" — is a well-known cognitive-bias question: the intuitive
but wrong answer is $0.10 (which makes the bat only $0.90 more than the
ball, not $1.00). It's a good self-consistency demo precisely because a
model can occasionally land on the intuitive-but-wrong path at higher
temperature, giving the vote a real minority answer to outweigh instead of
a uniformly correct field every time.

`finalAnswer` is requested — and declared on the class — before
`reasoning`, not after. This isn't stylistic. An earlier version of this
lesson, tested against Gemini, asked for reasoning first and got back
long, looping self-verification text ("I am confident... ready to
output... let's make sure...") that ran on so long the model never got to
writing `finalAnswer` at all, leaving it an empty string on every sample
and the vote tallying "unanimous agreement" on nothing. Capping
`reasoning` at three sentences and asking for the answer first fixed both
problems at once: the responses come back fast, and `finalAnswer` is
guaranteed to already be committed before the model has any chance to
ramble. The series has since moved to OpenAI; this specific runaway
hasn't been re-observed there, but the field ordering and sentence cap
cost nothing and stay as cheap insurance against any model's tendency to
over-explain before answering.

Temperature stays at `0.8` for the same reason as Day 32 — the whole point
is that five independent samples can genuinely diverge. Where this lesson
differs is what happens after: Day 32's reducer takes divergence and
blends it into something new; this lesson's `ConsensusVoter` takes
divergence and picks whichever answer the samples agree on most, which
only works because the answer space here is a handful of discrete values,
not open prose.

## Expected Result

Running the program produces output resembling:

```
PROBLEM: A bat and a ball cost $1.10 in total. The bat costs $1.00 more than the ball...

--- Sample 1 (answer: 0.05) ---
Let the ball's cost be L and the bat's cost be B. We have B + L = 1.10 and
B = L + 1.00. Substituting gives 2L + 1.00 = 1.10, so L = 0.05.

--- Sample 2 (answer: 0.05) ---
[similar reasoning, same answer]

--- Sample 3 (answer: 0.05) ---
[similar reasoning, same answer]

--- Sample 4 (answer: 0.05) ---
[similar reasoning, same answer]

--- Sample 5 (answer: 0.05) ---
[similar reasoning, same answer]

--- Vote Tally ---
  0.05: 5 vote(s)

--- CONSENSUS ANSWER ---
0.05
```

Every sample landing on the correct `0.05` is a legitimate outcome — it
means the model reliably reasons through this problem correctly at this
temperature. On other runs, one sample may land on the intuitive-but-wrong
`0.10`; when that happens, the tally shows something like `0.05: 4 vote(s)`
and `0.10: 1 vote(s)`, and the consensus answer is still `0.05` — the
minority's mistake gets outvoted rather than corrupting the result the way
it would if it were the only sample taken.
