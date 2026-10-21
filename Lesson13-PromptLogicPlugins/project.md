# Day 13 — Prompt Logic Plugins

## What this project builds

A console app that renders a customer-support prompt for a sample "Gold
tier" user (`Alice`), sends it to OpenAI's gpt-4.1-mini via Semantic Kernel, and
prints the AI-generated response. The interesting part isn't the AI call
itself — it's how the prompt gets assembled: instead of writing loops and
conditionals directly inside the prompt template string, the template calls
out to two native C# plugin functions and splices their return values into
the rendered text.

## Why it matters in the series

Earlier lessons write prompt templates as plain strings with `{{$variable}}`
substitution. As soon as a prompt needs a loop (formatting a list of
purchases) or a conditional (choosing tone/discount by membership tier),
cramming that logic into template syntax gets awkward fast. This lesson
introduces the pattern that scales: push the "if" and "for-each" logic into
ordinary, testable C# methods, and keep the prompt template itself limited to
variable substitution and function calls. It's the conceptual bridge between
"just formatting a string" (Days 1-12) and the fuller native-plugin /
function-calling material used from Day 4 onward and revisited more deeply
later in the series.

## Core concepts taught

- **Native function plugins** (`[KernelFunction]` / `[Description]`) — turning
  ordinary C# methods into things the Kernel's template engine (and, later,
  automatic function-calling) can invoke by name.
- **Registering plugins on the builder** — `builder.Plugins.AddFromType<UserHelperPlugin>()`
  scans a class for `[KernelFunction]` methods and makes them callable as
  `PluginName.FunctionName` before the Kernel is built.
- **Calling plugin functions from inside a template** — the
  `{{UserHelperPlugin.GetTierInstructions $tier}}` syntax resolves a function
  call and inserts its return value at render time, before the prompt is ever
  sent to the model.
- **Passing rich objects as template arguments** — the `user` argument is a
  full `UserProfile` record, not a string; the template engine matches it to
  the plugin function's declared parameter type instead of requiring
  pre-stringified input.
- **Records as lightweight data models** — `Product` and `UserProfile` show
  how C# `record` types make convenient, immutable data carriers for the
  objects plugin logic operates over.
- **Raw string literals** (`"""..."""`) for writing a clean, multi-line prompt
  template without verbatim-string escaping quirks.
- **Separating logic from prompt text** — the lesson's central idea: keeping
  the prompt template declarative and readable while the actual branching and
  iteration lives in regular, unit-testable C#.

## Cleanup notes

- **Fixed:** `kernel.InvokePromptAsync(promptTemplate, arguments)` was
  unguarded. Because this template calls two plugin functions during
  rendering, a rendering-time exception (e.g., from `GetPurchaseList` or
  `GetTierInstructions`) would have been just as fatal as a network/API
  failure, crashing the whole console app with a raw stack trace instead of a
  readable message. Wrapped the call in a `try`/`catch`, matching the same
  pattern already used in Day 2 (Summarizer) and other lessons in this series.
- **Left as enhancement opportunities (not defects):** the missing.md
  analysis also flagged things like adding null-argument guard clauses inside
  the plugin methods, adding unit tests for `GetPurchaseList`/
  `GetTierInstructions`, replacing the `Tier` string with an enum, expanding
  tier handling beyond Gold/default, and adding cancellation-token support.
  None of these block the tutorial as written (the sample data never
  exercises the null/unknown-tier paths), so they were intentionally left
  alone as production-hardening ideas rather than applied as fixes.
