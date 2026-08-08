# Day 36 — Closed-Loop Plan-and-Execute

## Overview

This lesson gives a Planner a goal, runs its plan against a simulated
environment that can reject a step for a reason the Planner had no way of
knowing in advance, and feeds that failure back so the Planner regenerates
the *entire plan* - not just the one step that failed. It extends Day 28's
PlannerAgent with the piece it was missing: a failure that changes strategy,
not just a retry.

## Prerequisites

- **Day 28 (Capstone-Plugins)** - for contrast. Day 28's PlannerAgent emits
  a task list once; nothing in that lesson ever regenerates it. This lesson
  adds exactly that missing loop.
- **Day 29 (Capstone-OrchestrationAndUI)** - for contrast. Day 29's Reviewer
  loop rejects one *task* and asks the same agent to redo that one task -
  task-level rework. This lesson's loop discards the whole plan and asks
  the Planner to rethink the approach - plan-level rework. Both are valid
  closed loops; they operate at different granularities.
- **Day 7, Day 31-35** - structured JSON output via `ResponseSchema`, and
  defensive parsing with a safe fallback. Reused here for the plan itself.

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

**Task-level rework vs. plan-level replanning.** Rejecting one step and
asking for a redo (Day 29) assumes the plan's *shape* was already correct
and only the execution of one piece was bad. Replanning assumes the
*shape* itself might be wrong - a step is missing, steps are in the wrong
order, or an approach needs to change - and asks the same agent that wrote
the original plan to reconsider it entirely, with the failure as new
evidence.

**The environment has to know something the Planner doesn't.** For a
replanning demo to mean anything, the first plan has to have a genuine,
reproducible reason to fail - not a coin flip. `SimulatedDeploymentEnvironment`
encodes one undocumented operational rule (a schema migration step fails
unless an earlier step pauses a background job) that's never mentioned in
the goal text. The Planner can only learn it by trying, failing, and reading
why.

**A failure message is the only channel back to the Planner.** The loop
doesn't hand the Planner a diff or a rule to append - it hands back one
string describing what happened, exactly the way a real deploy tool's error
output would look. Regenerating a good plan from that message alone is the
actual test of closed-loop planning; if the Planner needed the underlying
rule spelled out structurally, this wouldn't be replanning, it would be
configuration.

**Bounded like every other loop in this series.** `MaxAttempts = 3` caps
the plan/execute/replan cycle for the same reason Day 16's ReAct loop and
Day 31's self-reflection loop are capped - a Planner that can't converge
shouldn't retry forever.

## Full Walkthrough / Code

### `ExecutionPlan.cs`

```csharp
using System.Text.Json.Serialization;

namespace PlanAndExecute
{
    internal class ExecutionPlan
    {
        [JsonPropertyName("steps")]
        public List<string> Steps { get; set; } = new();
    }
}
```

### `ExecutionResult.cs`

```csharp
namespace PlanAndExecute
{
    internal class ExecutionResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
    }
}
```

### `SimulatedDeploymentEnvironment.cs`

The "world" the plan runs against. Its constraint is deterministic and
keyword-based, not an LLM judgment call, so the same plan always produces
the same result:

```csharp
namespace PlanAndExecute
{
    internal static class SimulatedDeploymentEnvironment
    {
        public static ExecutionResult RunPlan(ExecutionPlan plan)
        {
            bool archiverPaused = false;

            foreach (string step in plan.Steps)
            {
                string lower = step.ToLowerInvariant();

                if (lower.Contains("pause") && lower.Contains("archiver"))
                {
                    archiverPaused = true;
                    continue;
                }

                if ((lower.Contains("migrat") || lower.Contains("schema")) && !archiverPaused)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Message =
                            $"Step failed: \"{step}\". Migration failed: ArchiverJob is currently active " +
                            "and holds a lock on the orders table. ArchiverJob must be paused before any " +
                            "schema change step runs."
                    };
                }
            }

            return new ExecutionResult { Success = true, Message = "All steps executed successfully." };
        }
    }
}
```

### `Planner.cs`

```csharp
using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace PlanAndExecute
{
    internal class Planner
    {
        private readonly IChatCompletionService _chatService;

        public Planner(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<ExecutionPlan> GeneratePlanAsync(string goal, string? failureContext = null)
        {
            string failureBlock = failureContext is null
                ? string.Empty
                : $$"""

                    A PREVIOUS ATTEMPT AT THIS PLAN FAILED:
                    {{failureContext}}

                    Regenerate the full plan from scratch, incorporating whatever step is needed
                    to avoid this exact failure. Do not just repeat the failed step unchanged.
                    """;

            string prompt = $$"""
                You are a technical planner. Break the goal below into an ordered list of concise,
                concrete steps (5-8 words each). Output only the steps needed to accomplish the goal.
                {{failureBlock}}

                GOAL:
                {{goal}}

                Output ONLY valid JSON matching this schema:
                {
                    "steps": ["step 1", "step 2", "..."]
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.3,
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(ExecutionPlan)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<ExecutionPlan>(response.Content ?? "{}") ?? Empty();
            }
            catch (JsonException)
            {
                return Empty();
            }
        }

        private static ExecutionPlan Empty() => new()
        {
            Steps = new List<string> { "Failed to parse plan; no steps generated." }
        };
    }
}
```

### `Program.cs`

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PlanAndExecute
{
    internal class Program
    {
        private const int MaxAttempts = 3;
        private const string Goal = "Migrate the orders database to the new schema with zero downtime.";

        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var planner = new Planner(chatService);

            Console.WriteLine($"GOAL: {Goal}\n");

            string? failureContext = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Console.WriteLine($"=== Attempt {attempt}: Plan ===");
                ExecutionPlan plan = await planner.GeneratePlanAsync(Goal, failureContext);

                for (int i = 0; i < plan.Steps.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {plan.Steps[i]}");
                }

                Console.WriteLine("\n=== Executing ===");
                ExecutionResult result = SimulatedDeploymentEnvironment.RunPlan(plan);

                if (result.Success)
                {
                    Console.WriteLine($"SUCCESS: {result.Message}");
                    return;
                }

                Console.WriteLine($"FAILURE: {result.Message}\n");
                failureContext = result.Message;

                if (attempt == MaxAttempts)
                {
                    Console.WriteLine("Exhausted replanning attempts without a successful execution.");
                    return;
                }
            }
        }
    }
}
```

## Explanation

The goal - "migrate the orders database... with zero downtime" - never
mentions an archiver job. That's deliberate: a planner asked to produce a
migration plan from general knowledge will almost always propose something
like backup → migrate → verify → cutover, which is a perfectly reasonable
plan that has no way of anticipating one specific team's internal tooling
quirk. That's exactly the situation closed-loop planning exists for: not
"the agent made an error," but "the agent's plan was reasonable and still
failed against reality," which is a normal, expected outcome in real
infrastructure work.

Note what `GeneratePlanAsync` does with `failureContext`: it doesn't append
a `Fix: pause the archiver` instruction, and the code never touches the
plan itself once a failure comes back. It hands the *entire* natural-
language failure message back to the model and asks for a full
regeneration. The Planner has to read "ArchiverJob... holds a lock... must
be paused before any schema change step runs" and independently arrive at
inserting a pause step early and a resume step late - which is what makes
this closed-loop *planning* and not a scripted patch.

## Expected Result

A real run produced this exact transcript:

```
GOAL: Migrate the orders database to the new schema with zero downtime.

=== Attempt 1: Plan ===
  1. Create new schema tables and columns
  2. Implement dual-writes to old and new
  3. Backfill historical data to new schema
  4. Verify data consistency across schemas
  5. Switch read operations to new schema
  6. Monitor system stability and performance
  7. Decommission old database schema components

=== Executing ===
FAILURE: Step failed: "Create new schema tables and columns". Migration failed: ArchiverJob is
currently active and holds a lock on the orders table. ArchiverJob must be paused before any
schema change step runs.

=== Attempt 2: Plan ===
  1. Pause ArchiverJob process.
  2. Create new schema tables and columns.
  3. Backfill historical data to new schema.
  4. Enable dual writes to both schemas.
  5. Deploy applications reading from new schema.
  6. Verify data consistency and application health.
  7. Cutover all applications to new schema.
  8. Deprecate and drop old schema tables.
  9. Resume ArchiverJob process.

=== Executing ===
SUCCESS: All steps executed successfully.
```

Attempt 1's plan is reasonable and has no way of knowing about the archiver
constraint - it fails immediately. Attempt 2 isn't attempt 1 with a patch
inserted; it's a fuller plan that also adds a matching "resume" step at the
end, which nothing forced it to add - genuine replanning, not a scripted
retry. The exact wording will vary between runs, but the shape holds: a
first plan that fails for a stated reason, and a second plan that
addresses that reason without being told exactly how.
