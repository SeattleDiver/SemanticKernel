# Day 16 — Manual ReAct Loop

## What this project builds

A console app that reimplements, entirely by hand, the same "model calls a
tool to gather information" pattern used throughout the series — but without
Semantic Kernel's automatic tool execution. It sets `ToolCallBehavior` to
`GeminiToolCallBehavior.EnableKernelFunctions` (instead of the usual
`AutoInvokeKernelFunctions`), which lets the model *request* a function call
without the SDK actually running it. The program then owns every remaining
step itself: it inspects the model's response for a `FunctionCallContent`,
invokes the requested plugin method directly with `call.InvokeAsync(kernel)`,
and appends the result back into the `ChatHistory` as a tool "observation"
before asking the model to continue. This repeats in a capped loop (5
iterations) until the model stops requesting tools and produces a final
answer.

## Why it matters in the series

Every other lesson in this course treats tool-calling as a black box —
`AutoInvokeKernelFunctions` silently handles the request/execute/respond
cycle. This is the one lesson that opens the box. By making students write
the "request → execute → observe" plumbing themselves, it demystifies what
the framework has been doing automatically the whole time, and sets up the
more advanced multi-agent orchestration lessons later (Day 18, Day 19) that
reuse this same bounded-loop idea.

## Core concepts taught

- **`EnableKernelFunctions` vs. `AutoInvokeKernelFunctions`** — the model can
  ask for a tool, but Semantic Kernel will not run it on your behalf.
- **`FunctionCallContent`** — the structured (not free-text) representation
  of a model's tool request, read via `result.Items.OfType<FunctionCallContent>()`.
- **Manual invocation with `call.InvokeAsync(kernel)`** — the exact step that
  auto-invocation normally performs invisibly.
- **`FunctionResultContent` and the `AuthorRole.Tool` message** — how a tool's
  output is threaded back into the conversation so the model can see it.
- **The ReAct pattern (Thought → Action → Observation)**, driven here by a
  system prompt instructing the model to narrate its reasoning, with the C#
  loop supplying the observation half.
- **A stateful plugin registered via `AddFromObject`** — `ResearchPlugin`
  keeps a `_stepCount` field alive across calls, which requires registering
  the specific instance rather than letting Semantic Kernel construct its own
  via `AddFromType`.
- **A bounded iteration cap** — the `for (int i = 0; i < 5; i++)` loop is the
  lesson's built-in safety net against a model that never converges.

## Cleanup notes

- **Fixed:** the loop checked `if (string.IsNullOrEmpty(result.Content))
  continue;` *before* looking at `result.Items` for tool-call requests.
  Gemini frequently returns an empty `Content` string on turns where it is
  only requesting a function call (no narrated text), so this ordering meant
  a genuine tool-call request could be silently skipped every iteration,
  leaving the tool never invoked and the loop quietly spinning until the
  5-iteration cap — without ever demonstrating the ReAct cycle it exists to
  teach. The fix reorders the checks so `FunctionCallContent` is extracted
  first regardless of whether `Content` is empty, and switches from
  `history.AddAssistantMessage(result.Content)` to `history.Add(result)` so
  the full response (including any function-call items) is preserved in
  history rather than just its text. Verified with `dotnet build` — build
  still succeeds.
- **Left as enhancement opportunities (from `missing.md`), not defects:**
  richer logging/console framing per iteration, a final "success vs. max
  iterations reached" status message, a larger mock knowledge base in
  `ResearchPlugin.Search`, and cancellation-token support. None of these
  block or mislead a student following the tutorial as written, so they were
  left untouched per the cleanup scope (no retry/backoff, logging
  frameworks, or CancellationToken plumbing in this pass).
