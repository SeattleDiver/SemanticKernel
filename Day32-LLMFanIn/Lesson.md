# Day 32 — LLM-Based Fan-In

## Overview

This lesson completes the parallelization pattern that Day 7 left half
finished. Day 7 fanned *out* — N independent extraction calls ran against a
batch of feedback — but fanned back *in* with plain C#/LINQ (grouping,
counting). Here, N independent drafts are generated concurrently, and the
fan-in step is itself an LLM call: a synthesis editor that combines the
strongest elements of all N drafts into one answer better than any single
draft alone.

## Prerequisites

- **Day 7 (DataAnalyst)** — the fan-out half of this pattern: firing
  multiple independent model calls over a batch and collecting typed
  results. This lesson reuses that concurrency idea but replaces Day 7's
  deterministic LINQ aggregation with a model call.
- **Day 31 (Self-Reflection Loop)** — structured JSON output via
  `ResponseFormat`, and the convention of giving a synthesis/judgment step
  its own low temperature while generation runs hot. The same split is used
  here.
- Comfort with `Task.WhenAll` for concurrent async calls in C#.

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

**Fan-out is the easy half.** Running N model calls concurrently is just
`Task.WhenAll` over N independent `Task<string>` calls — nothing agentic
about it, it's ordinary async fan-out. Day 7 already proved this half of
the pattern works.

**Fan-in is the half that needs an LLM.** Once you have N *qualitative,
free-text* drafts — not N structured records with fields to group and
count — there is no deterministic way to combine them. "Take the best
analogy from draft 2 and the clearest phrasing from draft 1" is a judgment
call, not a LINQ query. That judgment call is itself a model invocation:
the reducer.

**Synthesis is not selection.** The reducer's instructions explicitly
forbid picking one draft verbatim or concatenating all of them — the point
of fan-in is a result that's *better than the best individual draft*, by
combining strengths, not a vote for a winner. (Voting *is* a legitimate
fan-in strategy — Day 33 builds exactly that — but it's a different
aggregation strategy, kept separate for exactly that reason.)

**Diversity comes from temperature, not from different prompts.** All three
drafts in this lesson use the identical task and an identical system
prompt; the only reason they differ from one another is
`Temperature = 0.9` at the generation step, plus the model's own sampling
randomness. This isolates fan-in's payoff cleanly — the drafts vary because
of sampling, not because they were told different things.

**The reducer explains itself.** `SynthesisResult.Rationale` asks the model
to name what it kept from each draft. This isn't required for the pattern
to work, but it's what makes the synthesis step legible on camera — without
it, "better than any individual draft" is just an assertion the audience
has to take on faith.

## Full Walkthrough / Code

### `SynthesisResult.cs`

The structured shape the reducer must return:

```csharp
using System.Text.Json.Serialization;

namespace LLMFanIn
{
    internal class SynthesisResult
    {
        [JsonPropertyName("synthesizedAnswer")]
        public string SynthesizedAnswer { get; set; } = string.Empty;

        [JsonPropertyName("rationale")]
        public string Rationale { get; set; } = string.Empty;
    }
}
```

### `Drafter.cs`

One independent, high-temperature generation call. Each call constructs
its own fresh `ChatHistory` — no drafter ever sees another drafter's
output, which is what keeps the fan-out calls genuinely independent:

```csharp
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    internal class Drafter
    {
        private readonly IChatCompletionService _chatService;

        public Drafter(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> GenerateDraftAsync(string task)
        {
            var history = new ChatHistory(
                "You are a technical writer. Answer the task directly, with no commentary.");
            history.AddUserMessage(task);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.9 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? string.Empty;
        }
    }
}
```

### `Reducer.cs`

The fan-in step. All N drafts are laid out in one prompt, and the model is
told explicitly what "synthesis" does and does not mean:

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    internal class Reducer
    {
        private readonly IChatCompletionService _chatService;

        public Reducer(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<SynthesisResult> SynthesizeAsync(string task, IReadOnlyList<string> drafts)
        {
            string draftsBlock = string.Join(
                "\n\n",
                drafts.Select((draft, index) => $"DRAFT {index + 1}:\n{draft}"));

            string prompt = $$"""
                You are a synthesis editor. {{drafts.Count}} independent writers each attempted
                the task below. Combine their strongest elements into ONE answer that is better
                than any single draft alone - do not just pick one draft verbatim, and do not
                simply concatenate them.

                TASK:
                {{task}}

                {{draftsBlock}}

                Output ONLY valid JSON matching this schema:
                {
                    "synthesizedAnswer": "The single combined answer.",
                    "rationale": "One short sentence per draft noting what, if anything, you kept from it."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                ResponseFormat = typeof(SynthesisResult)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<SynthesisResult>(response.Content ?? "{}")
                    ?? FallbackTo(drafts);
            }
            catch (JsonException)
            {
                return FallbackTo(drafts);
            }
        }

        private static SynthesisResult FallbackTo(IReadOnlyList<string> drafts) => new()
        {
            SynthesizedAnswer = drafts[0],
            Rationale = "Failed to parse synthesis JSON; falling back to the first draft."
        };
    }
}
```

### `Program.cs`

Fan out N drafts concurrently, then fan them in through one reducer call:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    internal class Program
    {
        private const int FanOutCount = 3;

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var drafter = new Drafter(chatService);
            var reducer = new Reducer(chatService);

            const string task =
                "Explain what a closure is to someone who has never programmed before, in one " +
                "paragraph, using a single real-world analogy.";

            Console.WriteLine($"TASK: {task}\n");

            var draftTasks = Enumerable.Range(0, FanOutCount)
                .Select(_ => drafter.GenerateDraftAsync(task));
            string[] drafts = await Task.WhenAll(draftTasks);

            for (int i = 0; i < drafts.Length; i++)
            {
                Console.WriteLine($"--- Draft {i + 1} ---\n{drafts[i]}\n");
            }

            SynthesisResult synthesis = await reducer.SynthesizeAsync(task, drafts);

            Console.WriteLine($"--- Synthesis Rationale ---\n{synthesis.Rationale}\n");
            Console.WriteLine("--- FINAL OUTPUT ---");
            Console.WriteLine(synthesis.SynthesizedAnswer);
        }
    }
}
```

## Explanation

`Task.WhenAll` over three `GenerateDraftAsync` calls is the entire fan-out
mechanism — three HTTP round-trips to OpenAI happen concurrently, not
sequentially, which is the actual "parallelization" half of the pattern
name. Nothing about that step is agentic; it's the same concurrency
primitive you'd use to fan out any three independent async calls.

The reducer prompt lays all three drafts out under labeled headers
(`DRAFT 1`, `DRAFT 2`, `DRAFT 3`) in a single call, rather than iterating
and asking the model to compare two at a time. A single call lets the model
weigh all candidates against each other at once, which is what makes the
result a genuine synthesis instead of a chain of pairwise merges that could
drift from the original task with each step.

Note what temperature is doing on each side of this pipeline: `0.9` on the
drafts maximizes the odds that sampling alone produces three meaningfully
different attempts; `0.2` on the reducer keeps the synthesis step close to
deterministic, so the same three drafts reliably produce a similar
synthesis rather than the reducer itself introducing fresh randomness on
top of what it's supposed to be consolidating.

## Expected Result

Running the program produces output resembling:

```
TASK: Explain what a closure is to someone who has never programmed before...

--- Draft 1 ---
Think of a closure like a backpack a function packs before it leaves home...

--- Draft 2 ---
A closure is like a photograph that keeps the room it was taken in frozen
in the background...

--- Draft 3 ---
Imagine a chef who writes down a recipe but also seals in a jar the exact
spices they had on the counter that day...

--- Synthesis Rationale ---
Draft 1's backpack analogy was the clearest for a total beginner, so it's
the base. Draft 3's "sealed in" phrasing better conveys permanence than
draft 1's "packs," so that language was borrowed. Draft 2's photograph
analogy was set aside as less concrete for a non-technical reader.

--- FINAL OUTPUT ---
Think of a closure like a backpack a function seals shut before it leaves
home, permanently keeping whatever variables were around at the time...
```

The exact analogies will vary between runs — sampling at `Temperature = 0.9`
guarantees that — but the shape is fixed: three genuinely different drafts,
a rationale that names specific, attributable borrowing from each one, and
a final answer that reads as a single coherent paragraph rather than a
patchwork of the three.
