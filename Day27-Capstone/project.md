# Day 27 — Universal Project Manager: Capstone Design & Architecture

## What this project builds

Day 27 kicks off the four-day capstone (Days 27→30): the **Universal Project
Manager (UPM)**, a multi-agent console app that will eventually turn a
high-level, rough project idea into a broken-down set of tasks, execute those
tasks with code-generating agents, and review the results. Day 27 doesn't run
any of that pipeline yet — it exists purely to lay down the *architecture*
the rest of the capstone builds on:

- `ProjectState` — a single, mutable shared state object (the "blackboard")
  holding `OriginalRequest`, `RefinedGoal`, and a `List<ProjectTask>`.
- `ProjectTask` — a plain data model for one unit of work (`Id`,
  `Description`, `AssignedTo`, `Result`, `IsCompleted`).
- `IProjectAgent` — the minimal contract (`Name`, `ExecuteAsync(ProjectState)`)
  every future specialist agent (Planner, Developer, Reviewer) must implement.
- `GoalRefinerAgent` — the first concrete agent, which takes the user's rough,
  conversational request and rewrites it into one clean technical goal
  sentence using a single direct `Kernel.InvokePromptAsync` call.

Running the app prompts for a rough project idea, runs it through
`GoalRefinerAgent`, and prints the resulting `ProjectState` — proving the
blackboard pattern works before any planning or coding agents are layered on
top of it in Day 28.

## Why it matters in the series

Every earlier lesson passed context around as a `ChatHistory` or a plain
string between a single caller and a single agent. That doesn't scale once
multiple specialized agents need to collaborate on the same piece of work.
Day 27 is the pivot point: it introduces a shared, mutable state object and
an interface-driven agent contract *before* any real agent logic is added, so
that Days 28–30 can add a Planner, a Developer, and an orchestrator that all
plug into the same architecture without needing to redesign it.

## Core concepts covered

- **Shared State ("Blackboard") Pattern** — one object every agent reads from
  and writes to, replacing point-to-point string/ChatHistory hand-offs.
- **Interface-Driven Agent Contracts** — `IProjectAgent` lets a future
  orchestrator treat every agent identically, regardless of what it does.
- **Goal Refinement as a Preliminary LLM Pass** — using the LLM purely as a
  text-normalization step, ahead of any planning.
- **Direct Prompt Invocation** — `_kernel.InvokePromptAsync(prompt, new
  KernelArguments(settings))`, a lighter-weight alternative to managing a
  `ChatHistory` for single-shot, non-conversational tasks.
- **Deterministic Output via Low Temperature** — `Temperature = 0.1` favors a
  consistent, professional rewrite over creative variation.
- **Computed State Properties** — `ProjectState.IsFullyCompleted` derives its
  value from `Tasks` rather than being tracked as a separately-updated flag.

## Cleanup notes

Two small, high-impact fixes were applied:

1. **`ProjectTask.Result` now defaults to `string.Empty`**, matching its
   sibling string properties (`Id`, `Description`, `AssignedTo`). Previously
   it was declared with no initializer under `<Nullable>enable</Nullable>`,
   a real nullable-reference inconsistency that left it as the only property
   without a safe default.
2. **`GoalRefinerAgent.ExecuteAsync` now wraps its `InvokePromptAsync` call in
   a try/catch.** Previously, any failure of the underlying Gemini call
   (network blip, rate limit, content filter) would throw unhandled and
   crash the entire console session with a raw stack trace. It now reports a
   short, friendly message and leaves `RefinedGoal` at its safe default
   instead of taking down the app.

The following items flagged during review were deliberately left as
**enhancement opportunities**, not defects, since they're production-hardening
concerns rather than tutorial-blocking bugs: retry/backoff and
`CancellationToken` plumbing for the LLM call, validating that the refined
goal is non-empty/well-formed, persisting `ProjectState` across process
restarts, unit tests for `GoalRefinerAgent`/`ProjectState`, and widening the
`internal` types to `public` for reuse from another assembly.
