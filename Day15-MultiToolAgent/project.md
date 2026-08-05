# Day 15 — Multi-Tool Agent (Tool Selection Across Multiple Plugins)

## What this project builds

A small console app that registers **two independent, unrelated plugins** —
`TimePlugin` and `WeatherPlugin` — on the same Semantic Kernel instance, then
asks the model a single question that genuinely needs both of them:
*"What time is it, and should I bring an umbrella in Salt Lake City today?"*

Earlier lessons in the series introduced one tool at a time, so "the model
calls the tool" wasn't much of a test — there was nothing else it could call.
This lesson is the first real demonstration of tool **selection**: the model
has to read a compound request, recognize it contains two separate
sub-questions, and decide on its own which plugin (or plugins) answers each
part, in what order, before it can produce a final answer.

## Why it matters in the series

This is the pivot point between "the framework can call a function" and "the
model can act like an agent that reasons about which of several capabilities
to use." Day 16 picks up right where this lesson leaves off by building the
same tool-invocation loop by hand (instead of letting the framework auto-run
it), so students can see exactly what `AutoInvokeKernelFunctions` was doing
for them here.

## Core concepts taught

- **Multiple plugin registration** — `builder.Plugins.AddFromType<TimePlugin>("Time")`
  and `AddFromType<WeatherPlugin>("Weather")` show a kernel hosting any number
  of independent plugins side by side, each under its own named namespace
  (`Time.GetLocalTime`, `Weather.GetWeather`).
- **`[KernelFunction]` / `[Description]` attributes** — these build the
  function "catalog" the model reasons over; the description text is what the
  model actually reads to decide whether/when to call a function.
- **Parameterized tool functions** — `GetWeather(string city)` shows Semantic
  Kernel extracting a model-supplied argument and marshaling it into a real
  C# method call.
- **Tool selection vs. tool execution** — the deliberately compound sample
  question forces the model to plan across two tools in a single turn, which
  is a materially harder task than invoking the one tool available.
- **`GeminiToolCallBehavior.AutoInvokeKernelFunctions`** — the single setting
  that makes the whole exchange "agentic": Semantic Kernel detects the
  model's function-call requests, invokes the matching C# methods, feeds the
  results back to the model, and returns only the final synthesized answer.
- **Console instrumentation inside plugin methods** — each tool prints an
  `[EXECUTING TOOL]` line as a side effect, making the otherwise invisible
  auto-invocation loop observable during a live demo.

## Cleanup notes

- Wrapped the `kernel.InvokePromptAsync(...)` call in `Program.cs` in a
  `try/catch`. Previously, any failure in the Gemini API call or in the
  auto-invoked tool round trips behind it (network error, API error, a tool
  throwing) would crash the entire console app with an unhandled exception.
  Since `AutoInvokeKernelFunctions` drives multiple hidden round trips per
  request, this was the single highest-risk unhandled-exception path in the
  lesson, so it now prints a friendly `Agent run failed: ...` message instead.
  The `Program.cs` code and the docx's Complete Code / Program.cs listing were
  both updated to match.
- Everything else flagged in the prior analysis (`missing.md`) — no
  interactive question loop, the hard-coded/fake weather logic, no
  cancellation token or timeout, no cap on auto-invoked tool round trips, and
  plugins writing directly to `Console` instead of returning data only — was
  deliberately left alone. Those are production-hardening or pedagogical-style
  choices (some are explicitly revisited by later lessons, e.g. Day 16's
  manual tool loop), not defects that would block or confuse someone working
  through this tutorial step by step.
