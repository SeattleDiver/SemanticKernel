# Day 38 — Vector-Indexed Episodic Memory

## Overview

This lesson stores facts learned across a conversation with their embedding
vectors, then retrieves for a new question by relevance instead of
recency. It upgrades Day 25's flat, recency-ordered fact list with exactly
the piece it was missing: a fact from turn one can still be the single most
useful thing to recall on turn six, and a recency window has no way to know
that.

## Prerequisites

- **Day 25 (LongTermMemory)** - for contrast. Day 25 extracts and persists
  facts, but retrieves them as a flat list with no ranking - effectively
  "everything, in the order it was learned." This lesson adds the ranking
  Day 25 never had: relevance to the current question.
- **Day 8 (RagAgent)** - the embedding and cosine-similarity mechanics here
  are the same hand-rolled approach, applied to a memory of past
  conversation instead of a static knowledge base.
- **Day 31-37** - structured JSON output, and treating a live run's actual
  output as the source of truth rather than an assumption about what a
  schema *should* produce.

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

**Recency and relevance are different rankings, and they diverge exactly
when it matters.** A recency window ("last N facts") is cheap and often
good enough, right up until the fact you actually need was learned long
ago and hasn't come up since. This lesson's whole demo is built to make
that divergence concrete: one fact is planted early and never repeated,
then a later question makes it the single most relevant thing in memory
despite being the oldest.

**Extraction and retrieval are separate concerns.** `FactExtractor` decides
*what's worth remembering* from a raw conversational turn - the same job
Day 25's extractor does. `EpisodicMemoryStore` decides *what's worth
retrieving* for a given query. Keeping them as separate classes means the
retrieval strategy (recency vs. relevance) can change without touching how
facts get captured in the first place.

**A schema with a bool before a string can silently lose the string.**
The first version of `FactExtractionResult` had two fields -
`isDurableFact` (bool) and `fact` (string), in that order. Against
**Gemini**, every single live call came back as `{"isDurableFact": true}`
with no `fact` field at all - not malformed JSON, just a *complete*, valid
object missing the field that mattered. The fix wasn't reordering fields
(Day 33's fix for a different bug); it was removing the bool entirely. A
single `fact` field, empty string meaning "nothing to remember," carries
the same information with one field instead of two whose interaction
apparently confused Gemini's structured-output path. The series has since
moved to OpenAI, and this specific failure hasn't been re-tested against
it - OpenAI's `ResponseFormat = typeof(T)` schema mode may or may not have
shown the same behavior. Either way, the single-field design is worth
keeping: when two fields together produce a result neither would
individually, collapsing them into one is worth trying before anything
more elaborate.

**Keeping both retrieval strategies side by side is the actual lesson.**
`RetrieveByRecency` isn't dead code kept for comparison - printing both
outputs for the same query, in the same run, is what makes the upgrade
visible instead of asserted.

## Full Walkthrough / Code

### `EpisodicFact.cs`

```csharp
namespace EpisodicMemory
{
    internal class EpisodicFact
    {
        public string Content { get; set; } = string.Empty;
        public int TurnNumber { get; set; }
        public ReadOnlyMemory<float> Embedding { get; set; }
    }
}
```

### `FactExtractionResult.cs`

```csharp
using System.Text.Json.Serialization;

namespace EpisodicMemory
{
    internal class FactExtractionResult
    {
        [JsonPropertyName("fact")]
        public string Fact { get; set; } = string.Empty;
    }
}
```

### `FactExtractor.cs`

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace EpisodicMemory
{
    internal class FactExtractor
    {
        private readonly IChatCompletionService _chatService;

        public FactExtractor(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string?> ExtractAsync(string turnText)
        {
            string prompt = $$"""
                A user said the following in conversation. If it contains a durable fact about
                them worth remembering for future conversations (preferences, people, pets, job,
                plans, allergies, etc. - not small talk), restate that fact concisely in third
                person. Otherwise, use an empty string.

                USER MESSAGE:
                {{turnText}}

                Output ONLY valid JSON matching this schema:
                {
                    "fact": "the restated fact, or an empty string if there is none"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                ResponseFormat = typeof(FactExtractionResult)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                var result = JsonSerializer.Deserialize<FactExtractionResult>(response.Content ?? "{}");
                return string.IsNullOrWhiteSpace(result?.Fact) ? null : result.Fact;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
```

### `EpisodicMemoryStore.cs`

```csharp
using Microsoft.Extensions.AI;

namespace EpisodicMemory
{
    internal class EpisodicMemoryStore
    {
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingService;
        private readonly List<EpisodicFact> _facts = new();

        public EpisodicMemoryStore(IEmbeddingGenerator<string, Embedding<float>> embeddingService)
        {
            _embeddingService = embeddingService;
        }

        public async Task RememberAsync(string content, int turnNumber)
        {
            ReadOnlyMemory<float> vector = await _embeddingService.GenerateVectorAsync(content);
            _facts.Add(new EpisodicFact { Content = content, TurnNumber = turnNumber, Embedding = vector });
        }

        public async Task<IReadOnlyList<EpisodicFact>> RetrieveByRelevanceAsync(string query, int topK)
        {
            ReadOnlyMemory<float> queryVector = await _embeddingService.GenerateVectorAsync(query);
            return _facts
                .OrderByDescending(f => CosineSimilarity(f.Embedding.Span, queryVector.Span))
                .Take(topK)
                .ToList();
        }

        public IReadOnlyList<EpisodicFact> RetrieveByRecency(int topK)
        {
            return _facts.OrderByDescending(f => f.TurnNumber).Take(topK).ToList();
        }

        private static float CosineSimilarity(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
        {
            float dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
            {
                return 0;
            }

            return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }
}
```

### `Program.cs`

```csharp
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace EpisodicMemory
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            builder.AddOpenAIEmbeddingGenerator("text-embedding-3-small", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var embeddingService = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();

            var extractor = new FactExtractor(chatService);
            var memory = new EpisodicMemoryStore(embeddingService);

            string[] turns =
            {
                "My dog Biscuit is allergic to chicken, so I have to check every treat label.",
                "I'm planning a trip to Japan next spring.",
                "My favorite color is teal.",
                "I just started a new job at a marine biology lab.",
                "I like hiking on weekends when the weather is nice.",
            };

            Console.WriteLine("=== Recording conversation turns ===");
            for (int i = 0; i < turns.Length; i++)
            {
                int turnNumber = i + 1;
                string? fact = await extractor.ExtractAsync(turns[i]);

                if (fact is not null)
                {
                    await memory.RememberAsync(fact, turnNumber);
                    Console.WriteLine($"  Turn {turnNumber}: remembered - \"{fact}\"");
                }
                else
                {
                    Console.WriteLine($"  Turn {turnNumber}: nothing durable to remember.");
                }
            }

            const string query = "What treats should I avoid buying for my pet?";
            Console.WriteLine($"\n=== New query (turn {turns.Length + 1}) ===\n{query}\n");

            IReadOnlyList<EpisodicFact> byRecency = memory.RetrieveByRecency(2);
            Console.WriteLine("--- Retrieval by RECENCY (last 2 facts learned) ---");
            foreach (var fact in byRecency)
            {
                Console.WriteLine($"  [Turn {fact.TurnNumber}] {fact.Content}");
            }

            IReadOnlyList<EpisodicFact> byRelevance = await memory.RetrieveByRelevanceAsync(query, 2);
            Console.WriteLine("\n--- Retrieval by RELEVANCE (top 2 by cosine similarity to the query) ---");
            foreach (var fact in byRelevance)
            {
                Console.WriteLine($"  [Turn {fact.TurnNumber}] {fact.Content}");
            }
        }
    }
}
```

## Explanation

The five scripted turns are deliberately unrelated to each other except for
one thing: turn 1 (a pet allergy) is the only fact that has anything to do
with the eventual query ("what treats should I avoid buying for my pet?").
Turns 2 through 5 (a trip, a color preference, a new job, a hobby) are all
genuinely durable, memorable facts - they're just irrelevant to *this*
question. That combination is what makes the comparison fair: recency
retrieval doesn't fail because turns 2-5 are somehow bad facts, it fails
because "most recent" and "most relevant" are answering different
questions, and only one of them is the question actually being asked.

`RetrieveByRecency` and `RetrieveByRelevanceAsync` both exist on the same
`EpisodicMemoryStore` and get called back to back on the same data in
`Program.cs` - neither one is the "old way" being replaced in code, they're
both live, so the difference in their output is the whole demonstration.

## Expected Result

A real run produced this exact transcript (captured against Gemini before
this lesson's migration to OpenAI - the retrieval rankings and which turn
lands in which bucket are structural, driven by the scripted turns and
cosine similarity, and should hold under OpenAI too, but the exact
fact-restatement wording will differ):

```
=== Recording conversation turns ===
  Turn 1: remembered - "Their dog Biscuit is allergic to chicken."
  Turn 2: remembered - "The user is planning a trip to Japan next spring."
  Turn 3: remembered - "Their favorite color is teal."
  Turn 4: remembered - "The user started a new job at a marine biology lab."
  Turn 5: remembered - "The user likes hiking on weekends when the weather is nice."

=== New query (turn 6) ===
What treats should I avoid buying for my pet?

--- Retrieval by RECENCY (last 2 facts learned) ---
  [Turn 5] The user likes hiking on weekends when the weather is nice.
  [Turn 4] The user started a new job at a marine biology lab.

--- Retrieval by RELEVANCE (top 2 by cosine similarity to the query) ---
  [Turn 1] Their dog Biscuit is allergic to chicken.
  [Turn 3] Their favorite color is teal.
```

Recency retrieval surfaces turns 4 and 5 - a job and a hobby, neither of
which helps answer the question at all. Relevance retrieval correctly puts
the turn 1 allergy fact first, despite it being the single oldest fact in
memory - the exact upgrade this lesson exists to demonstrate. The second
relevance result varies more between runs (embeddings will rank the
remaining, genuinely-unrelated facts close together), but the top result
should reliably be the allergy fact every time.
