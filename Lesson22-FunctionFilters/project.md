# Day 22 — Prompt and Function Filters (Middleware for AI)

## What this project builds

A console app that sends one scripted request through a Semantic Kernel
`Kernel` — "look up the current balance for customer ID CUST-778" — with two
cross-cutting filters wired in via dependency injection:

- **`PromptLoggingFilter`** (`IPromptRenderFilter`) intercepts the fully
  rendered prompt text immediately before it leaves the process for OpenAI.
- **`AuditFunctionFilter`** (`IFunctionInvocationFilter`) wraps every kernel
  function invocation with timing and before/after audit logging. That means
  each plugin call, and also the prompt itself: `InvokePromptAsync` compiles
  the prompt into a `KernelFunction` (no plugin name, a generated
  `InvokePromptAsync_<id>` function name) and invokes it, so the filter wraps
  the whole prompt call too.

The one call to `kernel.InvokePromptAsync` fans out into: the function filter
opening an outer audit pair around the prompt function, the prompt filter
(logging the rendered question, once), the real OpenAI call (which decides a
tool is needed), the function filter again (an inner audit pair timing the
`Database.GetCustomerBalance` call), the `SecureDatabasePlugin.GetBalanceAsync`
method itself, OpenAI's natural-language answer (the tool result goes back to
the model as part of the chat history, so the prompt is not rendered a second
time), and finally the outer audit pair closing with that answer as its
result.

## Why it matters in the series

Earlier days show plugins and tool-calling in isolation. Day 22 introduces
the idea that cross-cutting concerns — logging, auditing, security gating —
should not be hand-rolled inside every plugin. Instead, Semantic Kernel
exposes a middleware-style filter pipeline (directly analogous to ASP.NET
Core middleware) that observes or intercepts operations at well-defined
points in the request lifecycle, without touching plugin or prompt code at
all. This sets up later lessons (semantic routing, telemetry) that build on
the same filter/interception model.

## Core concepts taught

- **`IPromptRenderFilter`** — intercepts a prompt after variable substitution
  but before it is sent to the model; the natural place for logging or PII
  redaction.
- **`IFunctionInvocationFilter`** — wraps any `KernelFunction` invocation end
  to end (a native `[KernelFunction]` method or a prompt function), running
  code both before and after the real operation executes.
- **The `next` delegate pattern** — both filters do pre-work, call
  `await next(context)` to let the real operation proceed, then do
  post-work, mirroring ASP.NET Core middleware.
- **Filter registration via DI** — filters are added with
  `builder.Services.AddSingleton<...>()` *before* `builder.Build()`; once
  built, they apply automatically to every qualifying operation for the
  life of the kernel.
- **Separation of concerns** — `SecureDatabasePlugin` has zero logging or
  timing code; all observability lives in the filters.
- **Short-circuiting as a security hook** — because the filter controls
  whether `next(context)` is ever called, it can veto an operation entirely
  (e.g., reject a request without letting a sensitive plugin run).
- **Why AutoInvoke matters** — `ToolCallBehavior.AutoInvokeKernelFunctions`
  is what causes Semantic Kernel to actually execute the requested function
  (and therefore fire the inner `AuditFunctionFilter` pair around the tool)
  instead of just reporting that the model wants to call it.

## Cleanup notes

- Fixed a console-formatting bug in `PromptLoggingFilter.OnPromptRenderAsync`:
  the header line used `Console.Write` (no trailing newline) immediately
  before `Console.WriteLine(context.RenderedPrompt)`, so the rendered prompt
  ran on directly after the colon with no line break. Since this filter's
  entire purpose is producing readable observability output, the mangled
  output undercut the lesson. Changed the header to `Console.WriteLine` so
  the prompt text always starts on its own line, and synced the "Complete
  Code" listing in the docx to match.
- Fixed inconsistent logging in `AuditFunctionFilter`: the opening line
  logged `PluginName` and the closing line logged `Name`. Both lines now log
  the same identifier, `PluginName.Name` (or just the function name for the
  prompt function, which has no plugin), and the wording no longer calls
  every invocation a "tool", since the outer pair wraps the prompt function.
- Deliberately left as enhancement opportunities (not defects) per the prior
  `missing.md` analysis: no live demonstration of a
  filter actually blocking a call, no PII-redaction example in
  `PromptLoggingFilter` despite it being the stated flagship use case, the
  dead commented-out return-string line in `SecureDatabasePlugin`, and the
  lack of try/catch around `next(context)` in the function filter — the
  mock plugin never actually throws during this demo, so nothing here
  currently blocks or crashes the tutorial as written; these are all
  production-hardening/polish items rather than step-blocking bugs.
