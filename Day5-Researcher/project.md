# Day 5 — The Researcher (Native Model Grounding)

## What this project builds

A small console app that answers a question no LLM can answer from memory
alone: "What are the top 3 news headlines regarding space exploration
*today*?" Instead of adding a Semantic Kernel plugin (the Day 4 / Day 6
pattern), it flips on Gemini's own built-in Google Search grounding tool by
hand-crafting a `tools` entry inside `GeminiPromptExecutionSettings.ExtensionData`
and sending it straight through to the Gemini REST API.

## Why it matters in the series

Day 4 and Day 6 teach "give the model a tool" by writing a C# plugin that
Semantic Kernel calls on the model's behalf. Day 5 deliberately breaks that
pattern: the kernel that gets built has **zero** registered plugins, yet the
program still reaches the live internet. The point is to show students that
not every model capability needs — or has — a first-class Semantic Kernel
wrapper. Sometimes the right move is to drop down to the provider's native
API shape via an escape hatch, understand the trade-offs of doing so, and
know when two abstraction layers (SK's plugin tool-calling vs. a provider's
native tool-calling) can silently collide.

## Core concepts taught

- **Model grounding** — giving an LLM access to live, external information
  (via Google Search) so it can answer about events after its training
  cutoff instead of guessing.
- **Provider-native tools vs. Semantic Kernel plugins** — the `googleSearch`
  tool lives entirely inside Gemini; Semantic Kernel never sees or executes
  it, it only relays the request.
- **`ExtensionData` as an escape hatch** — how to inject arbitrary,
  provider-specific JSON into the request payload when SK's strongly-typed
  settings object doesn't (yet) expose a feature.
- **Why `ToolCallBehavior` must stay untouched here** — SK's own
  plugin/tool-calling machinery and Gemini's native grounding both populate
  the same `tools` JSON array. Setting `ToolCallBehavior` on this settings
  object would let SK overwrite the hand-built `googleSearch` entry with its
  own (empty) plugin tool array, silently disabling internet access with no
  error. This is the single most important gotcha in the lesson.
- **Low-level API shape awareness** — the anonymous object
  `new { googleSearch = new { } }` has to match Gemini's exact camelCase
  JSON key; escape-hatch features trade away SK's abstraction safety net for
  direct API fidelity.

## Cleanup notes

- **Fixed:** wrapped the `kernel.InvokePromptAsync` call in a `try`/`catch`
  (matching the pattern already established in Day 1/Day 2 of this series).
  Grounding adds an extra live search hop on top of the base chat-completion
  call, so there are more real failure points here (bad key, rate limit,
  quota, connectivity) than in a plain text-generation call — previously any
  of those would crash the whole program with a raw stack trace instead of a
  readable message. This was the only change that could actually stop a
  student cold while following the lesson, so it's the only code fix made.
- Verified the docx's "Complete Code" listing was a byte-for-byte match to
  `Program.cs` before editing, and re-synced it after the fix (see
  `Day5-Researcher.docx`, section 4).
- Left as **enhancement opportunities, not defects** (per `missing.md`):
  no inspection/printing of Gemini's `groundingMetadata` to prove grounding
  actually happened, no parameterized/CLI-supplied research topic, no
  graceful degradation if grounding returns no results, and no cost/quota
  callout for repeated grounded runs. These are production-hardening and
  pedagogy-enrichment ideas, not bugs — the lesson runs and teaches its core
  concept correctly without them.
