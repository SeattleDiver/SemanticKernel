# Day 19 — CoordinatorPro

## What this project builds

A console app that evolves the fixed-order, two-agent hand-off from Day 18
into a **Coordinator Pattern**: instead of the code hard-coding "Coder speaks,
then Auditor speaks," a dedicated meta-agent (the Coordinator) looks at the
conversation state on every loop iteration and decides which specialist —
a Coder or a Security Auditor — should act next, or whether the workflow is
done. The Coordinator is forced to answer in strict JSON (via
`GeminiPromptExecutionSettings.ResponseMimeType = "application/json"`) so its
routing decision can be deserialized straight into a `RoutingDecision` object
instead of being guessed from free text. The lesson also pairs this
orchestration logic with `RetryHandler.cs`, a small provider-agnostic
`DelegatingHandler` that retries transient HTTP failures (500/503/429) with
exponential backoff.

## Why it matters in the series

Day 18 taught students to wire two agents together with a fixed turn order.
This lesson is the natural next step: replace that hard-coded order with a
model-driven router, so the orchestration logic can react to what actually
happened in the conversation (code written? reviewed? approved? sent back for
changes?) rather than always running the same two steps. It also introduces
the idea that production agent orchestration needs to survive transient
upstream failures, not just reason correctly — hence the injectable
`HttpClient` wrapped in `RetryHandler`.

## Core concepts taught

- **The Coordinator (meta-agent) pattern** — a dedicated agent whose only job
  is to read history and pick the next speaker, replacing a fixed turn order
  with dynamic, state-driven routing.
- **Structured JSON output via `ResponseMimeType`** — forcing the model to
  return a JSON payload that deserializes reliably into a typed
  `RoutingDecision`, instead of parsing routing decisions out of free text.
- **Chain-of-thought before the decision** — the schema asks for a
  `reasoning` field ahead of `nextAgent`, modeling that asking a model to
  explain itself before committing to an answer tends to improve the answer.
- **The "Ghost Nudge"** — a throwaway `AuthorRole.User` message inserted only
  to satisfy Gemini's User/Assistant alternation rule, then explicitly
  removed so it never pollutes the history the specialists see later.
- **A reusable `CallAgent` helper** — factors the insert-persona / call /
  remove-persona / append-response sequence (duplicated by hand in Day 18)
  into one shared method used identically for the Coder and the Auditor.
- **HTTP-level resilience via a custom `HttpClient`** — shows that Semantic
  Kernel connectors accept an injectable `HttpClient`, so retry-with-backoff
  behavior can be added without touching any SK-specific code.
- **Deterministic routing via `Temperature = 0.0`** — reinforces that
  classification/routing-style tasks should be deterministic, not creative.

## Cleanup notes

- **Fixed:** `Program.cs` wrapped the Coordinator's
  `JsonSerializer.Deserialize<RoutingDecision>(...)` call in a `try/catch`.
  Although `ResponseMimeType = "application/json"` makes malformed JSON rare,
  a truncated response or a safety-filter block could still produce content
  that isn't valid JSON, and an uncaught `JsonException` there would crash
  the whole console session mid-workflow. The catch block now prints a clear
  `[ERROR]` message and ends the run gracefully instead. The docx's Complete
  Code listing and the Step 4 walkthrough prose (which had asserted the
  deserialize call "can be trusted to succeed") were updated to match.
- **Left as enhancement opportunities (from `missing.md`), not defects:**
  the `RetryHandler`'s lack of `Retry-After` header handling, jitter, and a
  max-delay cap; no `CancellationToken` threading from `Main` into the chat
  calls; no diagnostic logging when the Coordinator returns an unrecognized
  `nextAgent` value; the model string not actually being a "Pro"-tier Gemini
  model despite the lesson's title; a minor "betfore" comment typo; and no
  separate export of the final approved code. These are production-hardening
  or polish items, not things that block or confuse someone following the
  tutorial step by step.
