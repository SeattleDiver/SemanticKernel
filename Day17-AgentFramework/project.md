# Day 17 — Agent Framework (`ChatCompletionAgent`)

## What this project builds

An interactive, console-based travel-planning chatbot — but instead of
manually wiring together an `IChatCompletionService` and a `ChatHistory` the
way every earlier lesson in the series has, this lesson introduces Semantic
Kernel's Agent Framework via `ChatCompletionAgent`. The persona, the model
connection, and the generation settings are bundled into a single reusable
`travelAgent` object with a `Name` and `Instructions`, and the conversation
loop is driven by `travelAgent.InvokeAsync(chatHistory)` instead of talking
to the chat completion service directly.

## Why it matters in the series

This is the pivot point between "manual chat loop" lessons and "multi-agent"
lessons. Everything students have been doing by hand up to this point —
injecting a system persona, maintaining a `ChatHistory`, calling the model —
still happens here, but it's now wrapped in a purpose-built agent object.
That object model is exactly what Day 18 (multi-agent collaboration) and
Day 19 (coordinator patterns) build on, so this lesson is the bridge that
makes those later, more complex lessons possible.

## Core concepts taught

- **`ChatCompletionAgent`** — a higher-level type from
  `Microsoft.SemanticKernel.Agents` that bundles a `Name`, `Instructions`
  (persona), a `Kernel`, and default `Arguments` into one reusable unit,
  replacing the pattern of manually seeding a `ChatHistory` with a system
  message.
- **Agent `Instructions` as persona** — the persona now lives on the agent
  object itself rather than as the first message in the transcript,
  decoupling "who the agent is" from the conversation history.
- **Default `Arguments` on an agent** — execution settings like
  `Temperature` can be attached once to the agent rather than passed on
  every call.
- **`InvokeAsync` and `IAsyncEnumerable`** — invoking the agent returns an
  async stream of response messages, reflecting that an agent's turn can, in
  general, consist of more than one message.
- **The caller still owns history** — the agent doesn't silently manage
  conversation memory; the student explicitly appends each yielded message
  back into the shared `ChatHistory`, which is what makes the "shared
  history across multiple agents" pattern in Day 18 possible.

## Cleanup notes

- **Fixed:** wrapped the `await foreach (var message in
  travelAgent.InvokeAsync(chatHistory))` loop in a `try/catch`. Previously,
  any failure of the underlying Gemini call (rate limit, network blip,
  invalid key) would throw out of the loop unhandled and crash the entire
  console session, losing the whole conversation. The catch now reports the
  error and lets the `while(true)` loop continue so the user can try again.
  This was the one change that met the "would actually crash/block someone
  running the tutorial" bar.
- **Left as enhancement opportunities (not defects):** the other items
  in the prior gap analysis — no `CancellationToken` support in the
  interactive loop, no retry/backoff around the model call, no
  `[KernelFunction]` tools attached to the agent, no persistence of chat
  history across runs, and the minor persona/console text typos (`"bets
  destinations"`, `"Alwasy"`, `"Tarvel"`) — are all real observations but
  are production-hardening or content-polish items rather than things that
  block or break the lesson as written, so they were intentionally left
  untouched.
