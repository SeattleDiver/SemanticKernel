# Day 4 — The Calculator (Native Functions & Tool Calling)

## What this project builds

A small console app ("Math Agent") that gives Gemini a native C# calculator
to call instead of trying to do arithmetic itself. `MathPlugin.cs` exposes
four ordinary C# methods (`Add`, `Subtract`, `Multiply`, `Divide`) as
Semantic Kernel tools via the `[KernelFunction]` attribute, and
`Program.cs` wires that plugin into a `Kernel`, enables
`GeminiToolCallBehavior.AutoInvokeKernelFunctions`, and asks the model to
solve a multi-step apple word problem "using your tools." Running it prints
each native function call as it happens (`[NATIVE CODE EXECUTION] ...`),
followed by the model's final natural-language answer built from the exact
numeric results the plugin returned.

## Why it matters in the series

This is the course's first look at **native plugins and tool calling** —
the mechanism that lets an LLM go beyond generating text and actually
*do things* by invoking real code. Every later lesson that gives an agent
capabilities (file access, HTTP calls, multi-agent coordination) builds on
the pattern introduced here: describe a C# method well enough that the
model can decide when to call it, then let Semantic Kernel manage the
back-and-forth automatically.

## Core concepts taught

- **Native Plugins** — a plain C# class becomes an AI-callable toolset just
  by being registered with the kernel; no special base class or interface
  is required.
- **`[KernelFunction]`** — marks a method as callable by the model and
  gives it the name that shows up in the tool manifest.
- **`[Description]`** — attached to both methods and parameters, these
  strings are sent to the model as part of the tool manifest so it can
  decide which function matches its current need and which value goes into
  which argument.
- **`AddFromType<T>`** — registers a plugin class under a namespace
  (`"Math"`), making its functions addressable as `Math.Add`,
  `Math.Divide`, etc.
- **Auto tool-invocation** — `GeminiToolCallBehavior.AutoInvokeKernelFunctions`
  is what actually grants permission to execute a tool automatically;
  registering the plugin alone only makes the model aware it exists.
- **The reasoning loop** — `InvokePromptAsync` hides several silent
  round-trips: the model requests a function call, Semantic Kernel runs
  the real C# method, the result is fed back into the model's context, and
  the model keeps going until it has everything it needs for a final
  answer.

## Cleanup notes

- **Fixed:** `MathPlugin.Divide` now guards against division by zero
  before doing the division. Plain `double` division by zero doesn't
  throw — it silently returns `Infinity`/`NaN` — so without the guard the
  model would receive a non-numeric-looking result with no explanation
  and could hallucinate a reason for it. The method now throws a
  `DivideByZeroException` with a clear message, which Semantic Kernel can
  surface back to the model as a real tool error. The docx's "Complete
  Code" listing for `MathPlugin.cs` was updated to match.
- **Left as enhancement opportunities, not defects:** error handling
  around the tool-calling loop itself, plugin unit tests, a cap on
  tool-call round-trips, structured logging via `ILogger`, and a
  negative/adversarial word-problem example are all reasonable follow-ups
  (see the prior `missing.md` analysis) but are production-hardening or
  course-expansion items rather than things that block a student from
  successfully running this lesson as written.
