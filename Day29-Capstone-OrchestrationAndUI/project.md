# Day 29 — Capstone Implementation (Orchestration & UI)

## What this project builds

Day 29 is the third of the four-day Capstone arc (Day 27 → 28 → 29 → 30). It takes the
`UniversalProjectManager` console app — which by Day 28 had a `GoalRefinerAgent`,
`PlannerAgent`, and `DeveloperAgent` each called once, in a fixed order, straight out of
`Program.cs` — and turns it into a genuinely *orchestrated* multi-agent system. A new
`ProjectOrchestrator` class owns the entire agent lifecycle: it loops through the Refiner,
Planner, Developer, and a brand-new `ReviewerAgent`, repeating the cycle until every task is
both completed and quality-checked by the Reviewer, or a safety cap of five cycles is hit.
`Program.cs` no longer calls agents directly at all — it builds the list and hands control to
`orchestrator.RunProjectAsync(state)`.

## Why it matters in the series

This is the lesson where the series' recurring "blackboard" architecture (a shared
`ProjectState` object that agents read from and mutate, introduced on Day 27) gets its first
real feedback loop. Up to now, every agent only ever moved a task forward. Today, the Reviewer
can send a task *backward* — un-completing it and appending its own critique so the Developer
picks it back up next cycle. That single addition is what turns a linear pipeline into an
agentic loop, and loops need a termination story. Day 30 will reuse this exact orchestrator
inside a web UI, so getting the cycle-bounding and state-mutation patterns right here matters
for what comes next.

## Core concepts taught

- **The Orchestrator pattern** — `ProjectOrchestrator` depends only on `IProjectAgent` and
  knows nothing about what each agent does. It just calls `ExecuteAsync(state)` on every agent,
  in registration order, once per cycle — decoupling "how the workflow runs" from "what each
  step does."
- **The Reviewer / rework loop** — `ReviewerAgent` inspects every task where
  `IsCompleted == true`, asks the LLM whether the code satisfies the task, and on rejection
  flips `IsCompleted` back to `false` so the Developer's own pending-task filter
  (`!t.IsCompleted`) picks it back up next cycle.
- **Shared mutable state as the only communication channel** — there's no message bus between
  Developer and Reviewer; the Reviewer injects its critique straight into the field the
  Developer's next prompt reads from.
- **Bounded iteration** — `maxCycles = 5` is the safety valve that keeps a persistently
  rejecting Reviewer from looping forever. `ProjectState.IsFullyCompleted` is the convergence
  check that lets the loop exit early.
- **Per-agent prompt tuning** — each agent sets its own `Temperature`, tuned to its job: low
  for consistency (Refiner/Planner/Developer), `0.0` for the Reviewer, whose verdict needs to
  be as deterministic as possible.

## Cleanup notes

Two real logic bugs were fixed in `ReviewerAgent.cs`:

- The rejection log line was a plain string literal missing its `$` prefix
  (`Console.WriteLine("... {task.Id} ... {review}")`), so it printed the literal placeholder
  text instead of the actual task ID and rejection reason. Fixed by adding the missing `$`.
- The rejection path overwrote `task.Description` with just `[REVIEWER FEEDBACK] {review}`,
  discarding the original task requirement the Developer needs on retry. Changed `=` to `+=`
  so the feedback is appended instead, keeping the original description intact.

Also fixed: `ProjectTask.Result` was declared `public string Result { get; set; }` with no
default, inconsistent with every sibling string property in the same class (all of which
default to `string.Empty`) and at odds with the project's `<Nullable>enable</Nullable>`
setting. Given a default of `string.Empty` to match.

The docx's "Complete Code" listing, the Step 5 walkthrough prose, and the "Expected Output"
section (which had previously reproduced the buggy console line verbatim, with a note
explaining it was a known bug) were all updated to match the fixed code.

Deliberately left as enhancement opportunities, not defects, per `missing.md`: no
`CancellationToken`/timeout support, no retry/resilience wrapping around the bare
`InvokePromptAsync` calls, no persistence of `ProjectState` across runs, no unit tests for the
orchestrator's convergence logic, and the latent (but cycle-bounded) risk of the Planner
re-adding tasks every cycle instead of planning once. Minor cosmetic typos in prompt/log
strings (e.g. "techincal", "Deos", "a string QA Reviewer") were also left alone since they
don't affect functionality or block the tutorial.
