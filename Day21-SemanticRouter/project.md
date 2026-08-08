# Day 21 — Semantic Routing (Intent Classification & Isolated Kernels)

## What this project builds

An interactive console app that reads free-text user requests, classifies
each one's intent (`TECH`, `BILLING`, or `GENERAL`) with a dedicated "router"
call to OpenAI forced to return strict JSON, and then dispatches the request
to one of two specialist agents. Each specialist runs on its own cloned
`Kernel` that has only its own tool registered — `TechSupportAgent` can call
`ResetPassword`, `BillingAgent` can call `GetBalance`, and neither can reach
the other's tool no matter how the request is phrased.

## Why it matters in the series

Earlier lessons hand one agent a single toolbox and let the model pick the
right function. That approach degrades as the toolbox grows — more tools per
call means more chances for "tool hallucination," and no isolation between
tools if one is sensitive. Day 21 introduces a scaling pattern: classify
intent cheaply up front, then hand off to a narrowly-scoped specialist. It's
the architectural building block that Day 22 (function filters) and the
multi-agent lessons build on.

## Core concepts taught

- **Intent classification via structured JSON** — `SemanticRouter.RouteAsync`
  sets `OpenAIPromptExecutionSettings.ResponseFormat = "json_object"`
  and `Temperature = 0.0` so the router's output is deterministic and
  machine-parseable instead of free-form text you'd have to pattern-match.
- **Chain-of-thought-first prompting** — the JSON schema asks for
  `"reasoning"` before `"intent"`. Models tend to emit JSON keys in the order
  shown in the prompt's schema, so this forces a justification before the
  model commits to a category, which measurably improves classification
  accuracy for free.
- **Kernel isolation via `Kernel.Clone()`** — `TechSupportAgent` and
  `BillingAgent` each clone the shared `baseKernel`, which keeps the same AI
  service registrations but starts with an empty plugin collection. Each
  agent registers only its own `[KernelFunction]`, so tool isolation is
  structural (the method literally isn't loaded), not just a prompt
  instruction a user could argue around.
- **Deserializing model output into a DTO** — `RouteDecision` uses
  `[JsonPropertyName]` attributes so `JsonSerializer.Deserialize<RouteDecision>`
  can map the router's raw JSON straight onto a typed object the rest of the
  app branches on.
- **Scoped `AutoInvokeKernelFunctions`** — once routing has already happened
  and each specialist has exactly one tool available, turning on full
  auto-invoke carries none of the "what if it calls the wrong tool" risk it
  would in a single shared-kernel design.

## Cleanup notes

Two small, high-impact fixes were applied to `SemanticRouter/`:

1. **Unhandled `JsonException` on malformed router output** —
   `SemanticRouter.RouteAsync` fed the router's raw response straight into
   `JsonSerializer.Deserialize<RouteDecision>` with no try/catch. If the model
   ever returned truncated or otherwise invalid JSON despite the strict
   `ResponseFormat` setting, the exception would propagate uncaught and
   crash the whole console loop mid-session. Wrapped the deserialization in
   a try/catch that falls back to `RouteDecision { Intent = "GENERAL" }`,
   matching the defensive fallback the code already used for a null result.
2. **Copy-paste bug in `BillingAgent.GetBalance`** — the `accountId`
   parameter was decorated `[Description("No account ID")]` instead of
   describing the parameter. Since `[Description]` is what the model reads
   to decide how to call a tool, this was a real (if subtle) plugin-authoring
   bug that could degrade tool-calling accuracy. Fixed to
   `[Description("The account ID")]`.

Everything else flagged in `missing.md` — retry/timeout policies around the
three OpenAI calls, per-specialist `ChatHistory` for multi-turn memory, a
mechanism to recover from router misclassification, real persistence/
validation behind the mock tools, and durable logging of routing decisions —
is legitimate production-hardening but was deliberately left alone as an
enhancement opportunity rather than a defect, since none of it blocks or
breaks the tutorial as written.
