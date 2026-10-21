# Day 9 — The Interviewer

## What this project builds

A console-based "mock technical interviewer" agent. It runs an
open-ended, multi-turn conversation where an AI interviewer (persona:
"senior .NET architect") asks the person running the console one
question at a time about Agentic AI and Semantic Kernel, probes vague
answers with follow-ups, and escalates difficulty as the candidate
answers correctly — continuing until the candidate types `exit`.

## Why it matters in the series

Days 1-8 introduced the mechanics of talking to a model through
Semantic Kernel (`Kernel`, `IChatCompletionService`,
`GetChatMessageContentAsync`, `ChatHistory`). Day 9 deliberately
introduces no new API surface. Instead it shows how far you can steer
a model's *entire* behavior — turn-taking discipline, follow-up
questioning, escalating difficulty, staying in character across many
turns — purely through how the system prompt is written, layered on
top of the same `ChatHistory` loop from Day 1. It's the first lesson
that treats prompt design itself as the core skill being taught.

## Core concepts

- **Persona-constrained system prompts** — the `ChatHistory` is seeded
  with a system message that reads like a small spec: a role, a goal,
  and five numbered behavioral rules the model is expected to follow
  for the rest of the conversation without those rules being repeated.
- **Consistent execution settings across a whole conversation** — a
  single `OpenAIPromptExecutionSettings` (`Temperature = 0.7`) instance
  is created once and reused on every call, keeping the interviewer's
  tone stable turn after turn.
- **Seeding a conversation with a scripted kickoff message** —
  `chatHistory.AddUserMessage(...)` is called once, before the loop and
  before any real console input, purely to give the model something to
  respond to so it produces its self-introduction and opening question.
- **The "write both sides back to history" discipline, sustained over
  a long loop** — every candidate answer and every interviewer
  response is appended back into `ChatHistory`, the same pattern from
  Day 1, now carried across an indefinitely long back-and-forth with an
  explicit `exit` condition instead of a fixed number of turns.

## Cleanup notes

Two fixes were applied to `Interviewer/Program.cs`, both because an
unhandled failure here would end the entire interview session
mid-conversation rather than let the tutorial's own loop degrade
gracefully:

- Wrapped both `GetChatMessageContentAsync` calls (the priming call
  before the loop, and the call inside the interview loop) in
  try/catch. A transient network error, rate limit, or content-filter
  rejection from OpenAI previously threw an unhandled exception and
  killed the whole session; now the priming call prints a message and
  exits cleanly, and the in-loop call prints a message, keeps the
  accumulated `ChatHistory` intact, and lets the candidate try their
  answer again.
- Added a null-guard around the priming call's `response.Content`
  before it's printed and passed to `AddAssistantMessage` (which
  requires a non-null string). The loop body already had this guard
  for later turns; the very first response was unguarded and could
  throw if the model ever returned an empty first message.

The `.docx` lesson document's Complete Code listing was updated to
match, and `dotnet build` was re-verified after each change.

Everything else called out in the prior `missing.md` analysis —
conversation persistence/transcript export, an interview-length limit
with a final scored summary, cancellation-token support, and
externalizing the interview topic/persona to config — was deliberately
left as a follow-up enhancement rather than a defect. None of those
would stop the project from building or running as documented; they're
production-hardening/feature ideas for a future revision, not bugs.
The prompt's minor typos and missing spaces between concatenated
string literals (called out in `missing.md` item 5) were also left
alone since they don't affect the model's behavior or the tutorial's
correctness.
