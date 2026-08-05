# Day 10 — The Code Generator

## What this project builds

A console app that turns the `ChatHistory` conversation loop from earlier
days into a persona-driven "developer agent." The student describes a class
or function in plain English; the agent replies with a single fenced C#
code block; and because every request and response is appended to the same
`ChatHistory`, the student can keep iterating in later turns ("now make it
async," "add null checks") and the agent revises the code it already wrote,
with full context of what came before.

## Why it matters in the series

This is the first lesson where the *system prompt's output-format rules*
matter as much as its persona. Earlier days shaped conversational behavior;
this one shapes machine-consumable output (code-block-only, no filler),
because this agent's output is meant to be consumed by something else —
literally, Day 11's Critic agent reviews whatever this one produces. It's
also the first place execution settings are pushed toward determinism
(`Temperature = 0.2`, `TopP = 0.1`) rather than variety, setting up the
contrast with Day 9's higher-temperature conversational agent and Day 11's
fully deterministic critic.

## Core concepts it teaches

- **Low-temperature / low-TopP settings for deterministic output** — narrowing
  token choices so the same request tends to produce the same reasonable
  code, rather than creative variation you don't want from a code generator.
- **Persona + output-format rules in the system prompt** — telling the model
  not just who it is but exactly how its output must be shaped, since a
  chat model's natural instinct is to wrap code in explanatory prose.
- **Prompt templating at the call site** — wrapping the student's raw input
  (`"a Fibonacci calculator"`) in a small instruction template before adding
  it to history, rather than sending it verbatim.
- **Multi-turn refinement through shared history** — the same `ChatHistory`
  pattern from Day 1/Day 9, applied to code: the agent can see and revise
  code it generated several turns earlier because it's still in context.

## Cleanup notes

- Wrapped the `GetChatMessageContentAsync` call in a `try`/`catch`. Previously
  any transient API error or safety-filter rejection during generation would
  throw unhandled and crash the entire session — losing every code snippet
  generated so far, since nothing was persisted to disk. Now a failed turn
  prints an error and lets the student keep going.
- Fixed two typos in the system prompt string that made the agent's own
  stated rules confusing to read: "Always user modern .NET 8 syntax" →
  "Always use modern C# syntax" (also dropping the stale ".NET 8" reference,
  since the project actually targets `net10.0`), and "Not conversational
  filter" → "No conversational filler." Synced the same wording change into
  the docx's Complete Code and Code Walkthrough sections.
- Left as deliberate enhancement opportunities (not defects): saving
  generated code to disk, validating/stripping the response for stray prose
  outside the code fence, compiling the generated code with Roslyn before
  showing it, cancellation-token support, and history/context trimming for
  long sessions. These are all reasonable follow-ups but are production-
  hardening or feature additions rather than things that block or confuse
  someone working through this lesson today.
