# Day 7 — The Data Analyst

## What this project builds

An extraction agent. A small batch of raw, messy customer-feedback
strings — the kind a support inbox actually receives — is fed to OpenAI
one at a time, but instead of asking for a conversational reply, every
call forces the model to answer in a fixed JSON shape (sentiment,
product, one-line summary, priority) via `ResponseFormat`. Each response is deserialized into a typed
`FeedbackAnalysis` object with `System.Text.Json`, and the run finishes
with a small aggregate report (counts by sentiment, a list of anything
flagged urgent) built entirely from LINQ over the typed results.

## Why it matters in the series

Every earlier lesson treats the model's output as text to print. This
is the first lesson that treats the model's output as *data* — the
payoff being that a C# program can filter, group, and count it the
moment it comes back, instead of a human reading through free text
looking for the negative reviews. It also introduces `ResponseFormat`,
which constrains the model to a specific object shape at the API level,
a stronger guarantee than the prompt-only JSON formatting used later in
Day 19's `RoutingDecision`.

## Core concepts taught

- **`ResponseFormat`** — passing a C# `Type` (`typeof(FeedbackAnalysis)`)
  directly turns on OpenAI's Structured Outputs mode and auto-generates a
  JSON schema constraining the model's response to that type's fields and
  types, rather than JSON-shaped prose that has to be extracted from a
  larger reply.
- **`System.Text.Json` deserialization to a typed object** —
  `JsonSerializer.Deserialize<FeedbackAnalysis>(...)`, with
  `[JsonPropertyName]` bridging the model's lowerCamelCase field names
  to idiomatic PascalCase C# properties.
- **Per-item fault isolation in a batch loop** — both the model call and
  the JSON deserialization are wrapped in their own try/catch inside the
  `foreach`, so one bad entry (a transient failure, a safety-filter
  block, or malformed JSON) is skipped and logged instead of aborting
  the entire batch.
- **Aggregating typed results with LINQ** — `GroupBy` and `Where` over
  the resulting `List<FeedbackAnalysis>` to produce counts and an urgent-
  items list, the concrete reason structured output is worth the extra
  setup over free text.

## Implementation notes

- The knowledge that `ResponseFormat` accepts a plain C# `Type` (rather
  than a hand-written JSON Schema string) and that
  `OpenAIPromptExecutionSettings` exposes it at all was confirmed by
  reading the XML docs shipped with the exact package version this repo
  pins (`Microsoft.SemanticKernel.Connectors.OpenAI` 1.78.0) — it is not
  assumed from memory.
- The batch loop calls the model once per feedback entry rather than
  asking for a JSON array of all four analyses in one call. A single
  object per call keeps the `ResponseSchema` type simple
  (`typeof(FeedbackAnalysis)` instead of a wrapper array type) and keeps
  the per-item error handling from the bullet above meaningful — a
  single malformed array response would otherwise lose the whole batch
  instead of just one entry.
- No retry/backoff, no `CancellationToken`, and no persistence of the
  analyzed results to disk — consistent with how early Phase 1 lessons
  in this series are scoped; those are reasonable production-hardening
  additions for a later lesson, not gaps in this one.
