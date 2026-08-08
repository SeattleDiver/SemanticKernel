# Day 5 — The Researcher (Native Model Grounding)

## What this project builds

A small console app that answers a question no LLM can answer from memory
alone: "What are the top 3 news headlines regarding space exploration
*today*?" Instead of adding a Semantic Kernel plugin (the Day 4 / Day 6
pattern), it calls OpenAI's dedicated web-search model,
`gpt-4o-mini-search-preview`, directly against the Chat Completions REST
API with an empty `web_search_options` object in the request body.

## OpenAI migration note (breaking change from the original Gemini lesson)

Gemini exposed grounding as a `googleSearch` tool flag that could, in
principle, be set on its general-purpose chat model. OpenAI has no
equivalent flag for a general-purpose Chat Completions model: the
`web_search` tool that looks like a direct analog only exists on the newer
Responses API, and as of mid-2026 is documented in OpenAI's own developer
community as intermittently returning HTTP 500 server errors specifically
when paired with `gpt-4.1-mini` — not something to build a teaching example
on. The current best-supported alternative is `gpt-4o-mini-search-preview`,
a model where grounding is baked in rather than toggled by a tool flag.
Semantic Kernel's OpenAI connector has no strongly-typed wrapper for the
`web_search_options` field this model needs, so — exactly as the original
Gemini lesson did for its own connector's gap — the Kernel is still built
for series consistency, but the actual grounded call bypasses it entirely
and goes straight to OpenAI's REST API.

## Why it matters in the series

Day 4 and Day 6 teach "give the model a tool" by writing a C# plugin that
Semantic Kernel calls on the model's behalf. Day 5 deliberately breaks that
pattern: the kernel that gets built has **zero** registered plugins, yet the
program still reaches the live internet. The point is to show students that
not every model capability needs — or has — a first-class Semantic Kernel
wrapper. Sometimes the right move is to drop down to the provider's native
API shape via an escape hatch, understand the trade-offs of doing so, and
know that a provider's grounding support can be a completely different
shape (a dedicated model) than what a previous provider offered (a tool
flag on any model).

## Core concepts taught

- **Model grounding** — giving an LLM access to live, external information
  (via web search) so it can answer about events after its training
  cutoff instead of guessing.
- **Provider-native capabilities vs. Semantic Kernel plugins** — web search
  here is a property of which OpenAI model you call, not something
  Semantic Kernel sees, wraps, or executes.
- **Escape-hatching around a connector gap** — how to issue a direct REST
  call against a provider's API when SK's strongly-typed settings object
  doesn't (yet) expose a feature the lesson needs.
- **Why `temperature`/`top_p` must stay out of this request** — OpenAI's
  search-preview models reject sampling parameters outright; a request that
  includes them fails with an `unsupported_parameter` error. This is the
  single most important gotcha in the OpenAI version of this lesson.
- **Low-level API shape awareness** — the request body's `web_search_options`
  key and the model choice itself have to match OpenAI's documented shape
  exactly; escape-hatch features trade away SK's abstraction safety net for
  direct API fidelity.

## Cleanup notes

- **Fixed:** wrapped the grounded REST call in a `try`/`catch` (matching the
  pattern already established in Day 1/Day 2 of this series). Grounding adds
  an extra live search hop on top of the base chat-completion call, so there
  are more real failure points here (bad key, rate limit, quota,
  connectivity) than in a plain text-generation call — any of those would
  otherwise crash the whole program with a raw stack trace instead of a
  readable message.
- Left as **enhancement opportunities, not defects**: no inspection/printing
  of the response's citation annotations to prove grounding actually
  happened, no parameterized/CLI-supplied research topic, no graceful
  degradation if grounding returns no results, and no cost/quota callout for
  repeated grounded runs (OpenAI bills web search per tool call, separately
  from token usage). These are production-hardening and pedagogy-enrichment
  ideas, not bugs — the lesson runs and teaches its core concept correctly
  without them.
