# Day 20 — Human-in-the-Loop

## What this project builds

A console application that drafts a corporate email announcement with OpenAI
and refuses to let that draft go anywhere without an explicit human
"APPROVED" verdict. `AiWorker` owns the model interaction (persona injection,
generation, cleanup); `HumanGatekeeper` owns the console I/O that blocks
until a person types something; `Program.cs` wires the two together into a
revise-review loop bounded by a `maxRevisions` counter so it can never spin
forever.

## Why it matters in the series

Every earlier lesson in this series builds agents that act autonomously.
Day 20 is the checkpoint that says: some actions (sending an email, approving
a legal document, writing to a database) should never be fully autonomous,
no matter how good the model's own self-assessment is. It introduces the
Human-in-the-Loop (HITL) governance pattern that later, more complex
orchestrations (multi-agent, coordinator) can drop in as a safety gate.

## Core concepts taught

- **HITL Governance** — a workflow stage that only advances on a
  deterministic, human-issued signal (`"APPROVED"`), not on the AI's own
  judgment of task completion.
- **Separation of Concerns** — drafting logic (`AiWorker`) and
  human-review I/O (`HumanGatekeeper`) live in their own classes so
  `Program.cs` reads as a short orchestration script instead of a tangle of
  prompt and console code.
- **Persona Swapping via ChatHistory Mutation** — `AiWorker` inserts a
  system message at index 0 immediately before calling the model and
  removes it right after, so the persisted `ChatHistory` only ever contains
  the real user/assistant exchange.
- **Feedback-as-Context Loop** — a rejected draft's human feedback is
  appended to `history` as a new user message, which keeps the conversation
  a clean, natural user/assistant back-and-forth with no extra "ghost
  message" engineering needed.
- **Bounded Loop Termination** — the `while` loop is guarded by both
  `isApproved` and `currentRevision < maxRevisions`, guaranteeing the
  program terminates even if a human never types "APPROVED".
- **Records for Structured Results** — `ReviewResult` is a C# `record`
  carrying both the approval flag and the feedback text out of
  `HumanGatekeeper.ReviewDraft` as one immutable value.

## Cleanup notes

- **Added error handling around the Gemini call.** `AiWorker.GenerateDraftAsync`
  previously called `_chatService.GetChatMessageContentAsync(history)` with
  no try/catch, so any transient network failure, rate limit, or API error
  would throw straight out of `Main` and crash the whole approval workflow
  mid-review. `Program.cs` now wraps the call in a `try`/`catch`, prints a
  friendly error message, and stops the loop cleanly via a new
  `aiServiceFailed` flag instead of letting the process die with an
  unhandled exception.
- **Fixed a history-corruption bug in the same code path.** The persona
  system message injected at index 0 was only removed *after* the model
  call returned successfully. If that call threw, the system message would
  stay stuck at index 0 forever, corrupting every later revision attempt.
  The removal now happens in a `finally` block, so it always runs even on
  failure.
- The following items from the prior gap analysis (`missing.md`) were
  deliberately left as enhancement opportunities rather than defects: no
  cancellation/timeout on the review step, no audit trail for approvals, no
  input validation/reprompt on empty topic or feedback text, hardcoded
  persona/revision-limit configuration, cosmetic comment/string typos
  ("signle-responsibility", "Corporate Communications Directory", etc.), no
  persistence of the approved draft, and no `IHumanGatekeeper` abstraction.
  None of these block a student from building, running, or understanding
  the lesson as documented.
