# Day 34 — Multi-Agent Group Chat / Debate

## Overview

This lesson builds a debate among three agents where a moderator decides who
speaks next based on how the conversation is actually going - not a fixed
turn order. It replaces Day 18's alternating Copywriter/Editor hand-off with
genuine dynamic selection: the moderator can bring in whichever voice best
serves the discussion, and is explicitly steered away from letting any two
participants monopolize the floor.

**A note on how this lesson is built.** Semantic Kernel ships a native
`GroupChatOrchestration` API for exactly this pattern
(`Microsoft.SemanticKernel.Agents.Orchestration`, still experimental/preview).
It was tried first here and reproducibly failed live against **Gemini**:
agents returned empty responses on most turns, using both a custom
`GroupChatManager` and Microsoft's own built-in `RoundRobinGroupChatManager`
- ruling out this lesson's code as the cause. A search turned up a matching,
open report ([GitHub Discussion #13553](https://github.com/microsoft/semantic-kernel/discussions/13553))
describing the same failure "more than 50% of the time" - also reported
against a Gemini connector (Vertex AI), with no confirmation either way for
OpenAI. The series has since moved to OpenAI, and this specific failure
hasn't been re-tested against `GroupChatOrchestration` there; it may or may
not reproduce. The hand-rolled version below is kept regardless: it already
works, it depends on nothing but plain `Kernel`/`IChatCompletionService`
calls, and re-attempting the experimental orchestration path under a new
provider would be a fresh verification effort, not a straight provider
swap. The mechanics you'll learn - moderator-driven selection,
anti-repetition, forced rotation - are identical either way; only the
plumbing underneath differs.

## Prerequisites

- **Day 18 (MultiAgent)** - for contrast. Day 18's Copywriter/Editor
  exchange is a *fixed* order: strictly alternating, and only ever two
  participants. This lesson has three participants and no fixed order at
  all - the moderator picks fresh every turn.
- **Day 19 (CoordinatorPro)** - the moderator here is the same idea as
  Day 19's routing Coordinator (an LLM call that returns a structured
  routing decision), applied to picking a speaker instead of picking a
  specialist agent.
- **Day 7, Day 26, Day 31-33** - structured JSON output via
  `ResponseFormat`, defensive deserialization, and per-item error
  fallbacks. All reused here in the moderator's speaker-selection call.

## Setup

- .NET 10 SDK
- An OpenAI API key, available via the `OPENAI_API_KEY` environment variable
- NuGet packages - deliberately just the two already used throughout the
  series, since this lesson hand-rolls the pattern rather than pulling in
  SK's experimental orchestration packages:
  - `Microsoft.SemanticKernel` `1.78.0`
  - `Microsoft.SemanticKernel.Connectors.OpenAI` `1.78.0`

```
dotnet add package Microsoft.SemanticKernel --version 1.78.0
dotnet add package Microsoft.SemanticKernel.Connectors.OpenAI --version 1.78.0
```

## Core Concepts

**A moderator is just another structured-output LLM call.** There's no
special "moderator" primitive in Semantic Kernel. `DebateModerator` is a
plain class making the same kind of `ResponseFormat = "json_object"`
call every other lesson in this series makes - the only thing that makes it
a "moderator" is what it's asked to decide: who talks next, and eventually,
how the whole thing wrapped up.

**Excluding, not just discouraging.** The most recent speaker is removed
from the candidate list entirely before the moderator even sees a prompt -
it's not a "please don't pick them again" suggestion the model could ignore.
Testing showed this matters for a reason beyond fairness: asking the same
agent to continue immediately after its own uninterrupted turn, with no new
statement from anyone else in between, reliably produced empty replies -
there was nothing left for it to say. Removing that option removes the
failure mode along with it.

**Rotation has to be enforced, not just requested.** An early version of
the moderator's prompt said to "favor whoever was just challenged, or bring
in a quiet voice" - and it never once chose the second option, because in a
two-way argument someone was *always* just challenged. The fix tracks who
has spoken in the last two turns and, when anyone's been quiet that long,
tells the moderator it **must** pick one of them - overriding the
"just challenged" preference rather than merely competing with it. Soft
preferences that can always lose to a stronger-sounding alternative don't
change model behavior; hard constraints on the candidate pool or an
explicit override instruction do.

**Verify a "framework bug" before you believe it.** The empty-response
failure looked, at first, like it might be a mistake in this lesson's own
`GroupChatManager` subclass. Swapping in Semantic Kernel's own
`RoundRobinGroupChatManager` - zero custom code - and watching it fail
identically is what turned a suspicion into a conclusion. Don't blame your
own code for a framework's behavior, or blame a framework for your own
code's bug, without isolating which one is actually responsible.

## Full Walkthrough / Code

### `DebateParticipant.cs`

A participant is just data: a name, a one-line description the moderator
uses to pick between candidates, and the persona instructions used when
it's this participant's turn to speak.

```csharp
namespace GroupChatDebate
{
    internal class DebateParticipant
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Instructions { get; set; } = string.Empty;
    }
}
```

### `SpeakerDecision.cs`

The moderator's structured output for "who speaks next":

```csharp
using System.Text.Json.Serialization;

namespace GroupChatDebate
{
    internal class SpeakerDecision
    {
        [JsonPropertyName("nextSpeaker")]
        public string NextSpeaker { get; set; } = string.Empty;

        [JsonPropertyName("reason")]
        public string Reason { get; set; } = string.Empty;
    }
}
```

### `DebateModerator.cs`

Selects the next speaker and, once the debate ends, summarizes it:

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GroupChatDebate
{
    internal class DebateModerator
    {
        private readonly IChatCompletionService _chatService;

        public DebateModerator(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<SpeakerDecision> SelectNextSpeakerAsync(
            IReadOnlyList<DebateParticipant> participants, string transcript, string? lastSpeaker,
            IReadOnlySet<string> spokenInLastTwoTurns)
        {
            // The most recent speaker is removed from the candidate pool entirely, not just
            // discouraged by prompt wording - see Program.cs for why that matters.
            var candidates = participants.Where(p => p.Name != lastSpeaker).ToList();
            if (candidates.Count == 0)
            {
                candidates = participants.ToList();
            }

            string roster = string.Join("\n", candidates.Select(p => $"- {p.Name}: {p.Description}"));
            string transcriptText = transcript.Length == 0 ? "(the debate has not started yet)" : transcript;

            var overdue = candidates.Where(p => !spokenInLastTwoTurns.Contains(p.Name)).ToList();
            string overdueNote = overdue.Count > 0
                ? $"{string.Join(", ", overdue.Select(p => p.Name))} {(overdue.Count == 1 ? "hasn't" : "haven't")} spoken in a while - you MUST pick one of them now instead of continuing a two-way back-and-forth, even if someone else was just challenged."
                : "Everyone has spoken recently - pick based on who was just challenged.";

            string prompt = $$"""
                You are moderating a debate between 3+ participants. Decide who should speak next.
                A two-person debate isn't the goal here - a good moderator keeps rotating in every
                voice, not just letting two people volley back and forth. The most recent speaker
                is excluded below since they already just had a turn.

                {{overdueNote}}

                ELIGIBLE PARTICIPANTS:
                {{roster}}

                TRANSCRIPT SO FAR:
                {{transcriptText}}

                Output ONLY valid JSON matching this schema:
                {
                    "nextSpeaker": "the exact name of the participant who should speak next",
                    "reason": "one short sentence explaining the choice"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3,
                ResponseFormat = "json_object"
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                var decision = JsonSerializer.Deserialize<SpeakerDecision>(response.Content ?? "{}");
                if (decision is not null && candidates.Any(p => p.Name == decision.NextSpeaker))
                {
                    return decision;
                }
            }
            catch (JsonException)
            {
                // fall through to the fallback below
            }

            return new SpeakerDecision
            {
                NextSpeaker = candidates[0].Name,
                Reason = "Fallback: moderator response was unparseable."
            };
        }

        public async Task<string> SummarizeAsync(string transcript)
        {
            string prompt = $$"""
                The debate below has ended. Summarize the strongest point each side made and
                give a final verdict on the original question in 3-4 sentences total.

                TRANSCRIPT:
                {{transcript}}
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.3 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? "(no summary produced)";
        }
    }
}
```

### `DebateSpeaker.cs`

Generates one participant's turn. Each call is a fresh `ChatHistory` seeded
with that persona's instructions plus the transcript rendered as plain
text - not a long-lived per-agent thread:

```csharp
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GroupChatDebate
{
    internal class DebateSpeaker
    {
        private readonly IChatCompletionService _chatService;

        public DebateSpeaker(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> RespondAsync(DebateParticipant participant, string transcript)
        {
            var history = new ChatHistory(participant.Instructions);

            string prompt = transcript.Length == 0
                ? "The debate is starting. Make your opening point in 2-3 sentences."
                : $"TRANSCRIPT SO FAR:\n{transcript}\n\nIt's your turn. Respond directly to what was just said, in 2-3 sentences.";

            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.7 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? string.Empty;
        }
    }
}
```

### `Program.cs`

Drives the debate: each turn asks the moderator who's next, gets that
participant's response, and tracks recent speaker history so the moderator
can enforce rotation:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GroupChatDebate
{
    internal class Program
    {
        private const int MaxTurns = 6;

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var moderator = new DebateModerator(chatService);
            var speaker = new DebateSpeaker(chatService);

            var participants = new List<DebateParticipant>
            {
                new()
                {
                    Name = "OperationsLead",
                    Description = "Leads customer support operations and worries about coverage.",
                    Instructions =
                        "You are the Operations Lead. You are wary of a 4-day work week because " +
                        "customer response times and coverage matter most to you. Make one focused " +
                        "point per turn, directly engaging with what was just said.",
                },
                new()
                {
                    Name = "EngineeringManager",
                    Description = "Leads the engineering team and worries about burnout and focus time.",
                    Instructions =
                        "You are the Engineering Manager. You favor a 4-day work week because you " +
                        "believe it improves focus and reduces burnout. Make one focused point per " +
                        "turn, directly engaging with what was just said.",
                },
                new()
                {
                    Name = "FinanceAnalyst",
                    Description = "Tracks cost and output metrics and is skeptical of unproven changes.",
                    Instructions =
                        "You are the Finance Analyst. You are skeptical of a 4-day work week until " +
                        "it's proven not to hurt output-per-dollar. Make one focused point per turn, " +
                        "directly engaging with what was just said.",
                },
            };

            const string topic = "Should the company adopt a 4-day work week? Debate it.";
            Console.WriteLine($"TOPIC: {topic}\n");

            var transcriptLines = new List<string>();
            var speakerHistory = new List<string>();

            for (int turn = 1; turn <= MaxTurns; turn++)
            {
                string transcriptSoFar = string.Join("\n\n", transcriptLines);
                string? lastSpeaker = speakerHistory.Count > 0 ? speakerHistory[^1] : null;
                var spokenInLastTwoTurns = speakerHistory.TakeLast(2).ToHashSet();

                SpeakerDecision decision = await moderator.SelectNextSpeakerAsync(
                    participants, transcriptSoFar, lastSpeaker, spokenInLastTwoTurns);
                DebateParticipant nextSpeaker = participants.First(p => p.Name == decision.NextSpeaker);

                string response = await speaker.RespondAsync(nextSpeaker, transcriptSoFar);

                Console.WriteLine($"--- {nextSpeaker.Name} (moderator: {decision.Reason}) ---\n{response}\n");

                transcriptLines.Add($"{nextSpeaker.Name}: {response}");
                speakerHistory.Add(nextSpeaker.Name);
            }

            string verdict = await moderator.SummarizeAsync(string.Join("\n\n", transcriptLines));

            Console.WriteLine("--- FINAL VERDICT ---");
            Console.WriteLine(verdict);
        }
    }
}
```

## Explanation

`SelectNextSpeakerAsync` takes a `spokenInLastTwoTurns` set built by the
caller (`Program.cs`), not computed internally - the moderator doesn't
track its own history, `Program.cs` does, and hands the moderator exactly
what it needs each call. This keeps the moderator itself stateless: every
call is a fresh decision from the transcript and recency data it's given,
not from any memory of its own past choices.

The `overdueNote` string is where the anti-monopoly logic actually lives.
When someone has gone two turns without speaking, the prompt doesn't ask
the moderator to *consider* including them - it states a `MUST`, and
explicitly says this overrides the "just challenged" preference. Live
testing showed the softer, request-only version of this instruction never
once won out over "someone was just challenged" in a two-person argument,
because there's always a most-recently-challenged person to point to. A
constraint that can lose to a competing instruction in the same prompt will
lose it, eventually, every time it matters.

`DebateSpeaker.RespondAsync` never keeps a long-lived conversation thread
per participant - every call builds a brand-new `ChatHistory` from that
participant's instructions plus the transcript as plain text. This was the
direct fix for the empty-response bug found (against Gemini) in the
experimental `GroupChatOrchestration` path: a fresh, fully-specified prompt
every turn has no ambiguous "continue" state for the model to return
nothing for. Whether or not that specific bug reproduces under OpenAI, a
fresh prompt per turn is still the simpler, more predictable design - so
it's kept either way.

## Expected Result

Running the program produces output resembling:

```
TOPIC: Should the company adopt a 4-day work week? Debate it.

--- OperationsLead (moderator: OperationsLead hasn't spoken yet and can provide an initial perspective...) ---
From an Operations perspective, my primary concern is maintaining our current high standards
for customer response times and ensuring consistent coverage...

--- FinanceAnalyst (moderator: To ensure all voices are heard and to bring in the financial perspective...) ---
That's precisely the financial risk I'm concerned about. A drop in customer response times...

--- EngineeringManager (moderator: To ensure all voices are heard and prevent a two-way back-and-forth...) ---
I understand the focus on maintaining our critical metrics and output-per-dollar...

[... three more turns, rotating through all three participants ...]

--- FINAL VERDICT ---
The Operations Lead and Finance Analyst strongly argued that a compressed work week poses
significant financial and operational risks without a concrete plan guaranteeing consistent
5-day coverage and stable customer response times. Conversely, the Engineering Manager
contended that reduced burnout and improved focus from a 4-day week would enhance individual
productivity, enabling teams to creatively maintain or exceed service standards. The debate
highlights a crucial gap: while potential benefits exist, the lack of a proven, concrete
strategy for maintaining 5-day operational consistency and mitigating financial risks makes
proceeding currently untenable.
```

The moderator's stated reasons will vary between runs, but all three
participants should get pulled into the conversation across six turns - not
just two of them ping-ponging - and the reasons should visibly reference
either a direct challenge or someone having been quiet too long.
