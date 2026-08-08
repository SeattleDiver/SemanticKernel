# Day 31 — Self-Reflection Loop

## Overview

This lesson builds a single agent that generates a draft, critiques its own
output against the original task, and revises it — repeating until its own
critique is satisfied or a revision cap is hit. No second persona is
involved anywhere in the loop: every call comes from the same "self," just
wearing a different hat (Draft, Critique, Revise) each time it's invoked.
This closes the self-reflection gap in the series' orchestration pattern
coverage — the one where quality control comes from *within* a single
agent's own process, not from a second agent or a human.

## Prerequisites

- **Day 7** and **Day 26** — structured JSON output via
  `OpenAIPromptExecutionSettings.ResponseFormat`, deserialized defensively
  into a typed result. This lesson reuses that exact mechanism for the
  critique step.
- **Day 11 (Critic)** — for contrast. Day 11's critic scores a *fixed,
  external* code snippet once, with no loop and no connection back to
  whoever wrote it. This lesson's critique step targets the agent's *own*
  draft, and feeds back into another attempt.
- **Day 18 (MultiAgent)** — for contrast. Day 18 alternates two distinct
  personas (Copywriter, Editor) sharing one `ChatHistory`. This lesson uses
  exactly one persona; there is no hand-off to another role, and no shared
  conversational history between passes (see Core Concepts, below).
- General familiarity with `Kernel`, `IChatCompletionService`, and bounded
  iteration loops (Day 16's manual loop cap is the same idea applied here).

## Setup

- .NET 10 SDK
- An OpenAI API key, available via the `OPENAI_API_KEY` environment variable
- NuGet packages (already pinned to match the rest of this series so the
  whole repo builds against one consistent SDK surface):
  - `Microsoft.SemanticKernel` `1.78.0`
  - `Microsoft.SemanticKernel.Connectors.OpenAI` `1.78.0`

```
dotnet add package Microsoft.SemanticKernel --version 1.78.0
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI --version 1.78.0
```

## Core Concepts

**Self-reflection vs. critic vs. debate.** All three patterns involve a
"generate, then judge" step, but they differ in *who* does the judging and
*how many actors are on stage:

| Pattern | Actors | Judges | Loop? |
|---|---|---|---|
| Day 11 Critic | 1 fixed snippet + 1 critic | External critic | No |
| Day 18 MultiAgent | 2 personas | The other persona | Yes, fixed turn order |
| **Day 31 Self-Reflection** | **1 agent** | **Itself** | **Yes, self-terminating** |

Self-reflection is the degenerate, single-actor case: nothing new is
introduced except the discipline of splitting "produce" and "judge" into
two distinct calls against the same output, so the judgment isn't
contaminated by the momentum of having just written the thing.

**No shared conversation history.** `DraftAsync`, `CritiqueAsync`, and
`ReviseAsync` each start a fresh `ChatHistory` rather than appending to one
running conversation. This is deliberate: if the critique step could see
its own prior turns as a conversational thread, a model is prone to being
"agreeable" toward its earlier self, the same social bias that makes
chat-style self-review unreliable. Passing the draft and task in explicitly
as plain text — not as chat turns the model authored — forces each critique
to re-derive its judgment from the artifact alone.

**A critique is a structured verdict, not free text.** `SelfCritique`
exposes an `IsSatisfactory` check the loop reads as a field, not something
it infers by parsing prose. It's derived from a single `Feedback` string
rather than a separate boolean returned by the model — see Explanation for
why a second field turned out to be actively harmful here.

**Bounded iteration is the safety net, not the goal.** `MaxPasses = 3`
guarantees termination even if the critique step never reports
satisfaction. The loop always ships the latest draft — a capped
self-reflection loop that runs out of passes is not a failure state, it's
an intentional "good enough, move on" boundary.

## Full Walkthrough / Code

### `SelfCritique.cs`

The structured shape the critique step must return. `IsSatisfactory` is
computed in C#, not deserialized from the model - see Explanation:

```csharp
using System.Text.Json.Serialization;

namespace SelfReflectionLoop
{
    internal class SelfCritique
    {
        [JsonPropertyName("feedback")]
        public string Feedback { get; set; } = string.Empty;

        [JsonIgnore]
        public bool IsSatisfactory => string.IsNullOrWhiteSpace(Feedback);
    }
}
```

### `ReflectiveAgent.cs`

The three hats. `DraftAsync` and `ReviseAsync` are free-text generation
calls; `CritiqueAsync` is the one call constrained to structured JSON via
`ResponseFormat`:

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SelfReflectionLoop
{
    internal class ReflectiveAgent
    {
        private readonly IChatCompletionService _chatService;

        public ReflectiveAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> DraftAsync(string task)
        {
            var history = new ChatHistory(
                "You are a skilled writer. Produce a first draft that satisfies the task exactly. " +
                "Output only the draft itself, with no commentary.");
            history.AddUserMessage(task);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.7 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? string.Empty;
        }

        public async Task<SelfCritique> CritiqueAsync(string task, string draft)
        {
            string prompt = $$"""
                You are the same writer, reviewing your own draft before it ships. Judge it
                strictly against the original task - do not invent requirements that were
                never asked for.

                ORIGINAL TASK:
                {{task}}

                YOUR DRAFT:
                {{draft}}

                If the draft already satisfies the task exactly, respond with an empty string.

                Output ONLY valid JSON matching this schema:
                {
                    "feedback": "Specific, actionable feedback on what to fix, or an empty string if satisfactory."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                ResponseFormat = typeof(SelfCritique)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<SelfCritique>(response.Content ?? "{}")
                    ?? new SelfCritique { Feedback = "Failed to parse self-critique JSON." };
            }
            catch (JsonException)
            {
                return new SelfCritique { Feedback = "Failed to parse self-critique JSON." };
            }
        }

        public async Task<string> ReviseAsync(string task, string draft, string feedback)
        {
            string prompt = $"""
                You are the same writer, revising your own draft based on your own critique.
                Output only the revised draft, with no commentary on what changed.

                ORIGINAL TASK:
                {task}

                PREVIOUS DRAFT:
                {draft}

                YOUR OWN FEEDBACK TO ADDRESS:
                {feedback}
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.7 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? draft;
        }
    }
}
```

### `Program.cs`

The loop itself — draft once, then alternate critique/revise until
satisfied or capped:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SelfReflectionLoop
{
    internal class Program
    {
        private const int MaxPasses = 3;

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var agent = new ReflectiveAgent(chatService);

            const string task =
                "Write EXACTLY three sentences of product description for a noise-cancelling " +
                "travel mug that keeps drinks at a set temperature for 12 hours. It must name " +
                "the temperature-lock feature explicitly and end with a call to action.";

            Console.WriteLine($"TASK: {task}\n");

            string draft = await agent.DraftAsync(task);
            Console.WriteLine($"--- Draft 1 ---\n{draft}\n");

            for (int pass = 1; pass <= MaxPasses; pass++)
            {
                SelfCritique critique = await agent.CritiqueAsync(task, draft);

                if (critique.IsSatisfactory)
                {
                    Console.WriteLine($"--- Self-critique (pass {pass}) ---\nSatisfied. Stopping.\n");
                    break;
                }

                Console.WriteLine($"--- Self-critique (pass {pass}) ---\n{critique.Feedback}\n");

                if (pass == MaxPasses)
                {
                    Console.WriteLine("Reached the revision cap; shipping the latest draft as-is.\n");
                    break;
                }

                draft = await agent.ReviseAsync(task, draft, critique.Feedback);
                Console.WriteLine($"--- Draft {pass + 1} ---\n{draft}\n");
            }

            Console.WriteLine("--- FINAL OUTPUT ---");
            Console.WriteLine(draft);
        }
    }
}
```

## Explanation

The task is deliberately over-specified ("EXACTLY three sentences," a named
feature that must appear, a required closing call to action) so the first
draft has a realistic chance of missing at least one constraint — that's
what gives the critique step something genuine to catch on camera instead
of rubber-stamping pass 1.

`CritiqueAsync` is called with `Temperature = 0.0` while `DraftAsync` and
`ReviseAsync` run at `0.7` — the same deliberate split seen in Day 26's
target/judge temperatures. Generation benefits from variety; judgment
benefits from consistency. Running the critique cold makes the
`IsSatisfactory` flag a repeatable signal rather than a coin flip.

The loop caps at `MaxPasses = 3`, giving at most 3 drafts and 3 critiques.
Unlike Day 18's `while` loop (which watches for a literal `"APPROVED"`
string from a *different* persona), the exit condition here is read from
the agent's own critique of its own work — there's no second opinion to
wait on, so the loop can end the instant the agent is satisfied with
itself, or run out of patience with itself.

**Why `IsSatisfactory` isn't deserialized from the model.** The first
version of this lesson had `SelfCritique` ask for two fields -
`isSatisfactory` (bool) and `feedback` (string), mirroring how the schema
reads out loud. Forcing an unsatisfactory first draft to test the revision
path exposed a real bug against Gemini, the provider this lesson originally
shipped against: the model reliably returned `{"isSatisfactory": false}`
with no `feedback` field at all - a complete, valid JSON object, just
missing the one piece of information the loop actually needed to act on.
The fix - also used in Day 38 for the same failure shape - was to drop the
bool and derive it from whether `feedback` came back empty. One field
can't be partially missing in a way that silently breaks the loop; two
fields, it turns out, can. The series has since moved to OpenAI, whose
`ResponseFormat = typeof(T)` structured-output mode enforces *every*
serializable field in the schema as required - including computed,
get-only properties like `IsSatisfactory` picked up by reflection, not
just the ones with a `[JsonPropertyName]`. Left unguarded, that would have
reintroduced a two-field schema by accident and forced the model to emit
an undocumented `IsSatisfactory` value on every call, quietly defeating
the whole point of this section. `SelfCritique.IsSatisfactory` is marked
`[JsonIgnore]` specifically to keep the wire schema down to the one field
this design actually depends on.

## Expected Result

Running the program produces output resembling:

```
TASK: Write EXACTLY three sentences of product description for a noise-cancelling travel mug...

--- Draft 1 ---
Stay warm or cold for half your day with the Aegis travel mug...
[Two sentences shown; temperature-lock feature not named; no call to action]

--- Self-critique (pass 1) ---
The draft is only two sentences, not three. It never names the
temperature-lock feature explicitly. It has no call to action. Revise to
meet all three requirements.

--- Draft 2 ---
The Aegis travel mug keeps your drink locked at your chosen temperature
for a full 12 hours, thanks to its built-in temperature-lock seal.
Whether it's coffee at dawn or water on a midday hike, your drink stays
exactly how you like it, mile after mile. Grab yours today and never
settle for lukewarm again.

--- Self-critique (pass 2) ---
Satisfied. Stopping.

--- FINAL OUTPUT ---
The Aegis travel mug keeps your drink locked at your chosen temperature
for a full 12 hours, thanks to its built-in temperature-lock seal.
Whether it's coffee at dawn or water on a midday hike, your drink stays
exactly how you like it, mile after mile. Grab yours today and never
settle for lukewarm again.
```

The exact wording will vary between runs, but the shape is fixed: at least
one imperfect first draft, a specific and actionable self-critique, a
revision that addresses it, and a run that terminates on its own — not on
the `MaxPasses` cap — once the agent's own bar is cleared.
