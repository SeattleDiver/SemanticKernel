# Day 24 — Telemetry & Observability

## What this project builds

A console app that wires OpenTelemetry (OTel) into Semantic Kernel's own
built-in diagnostic instrumentation, then fires a single tool-calling prompt
- "What is the weather like in Seattle right now?" - through a kernel that
has a deliberately slow mock weather plugin (`SlowWeatherPlugin`) registered.
As the request runs, every prompt render, model call, and plugin invocation
that Semantic Kernel already tracks internally as a `System.Diagnostics
.Activity` is captured by an OTel `TracerProvider` and printed to the
console as a structured trace, complete with real elapsed-time durations.

## Why it matters in the series

Earlier lessons (notably Day 22's function filters) showed how to hand-write
instrumentation around specific calls. This lesson flips that: rather than
wrapping every operation yourself, you subscribe to the `ActivitySource`
Semantic Kernel already emits under the hood and get detailed,
provider-agnostic tracing for free, with zero changes to the plugin or
prompt code. It's presented as the series capstone because it's the payoff
of everything built up to this point - a single `InvokePromptAsync` call
now produces multiple visible spans (the initial LLM call, the plugin
execution, and the follow-up LLM call) with no manual bookkeeping.

## Core concepts taught

- **OpenTelemetry (OTel)**: the vendor-neutral observability standard for
  traces, metrics, and logs, used here via the `OpenTelemetry` and
  `OpenTelemetry.Exporter.Console` NuGet packages.
- **ActivitySource subscription**: `traceBuilder.AddSource("Microsoft
  .SemanticKernel*")` tells the tracer to listen to every diagnostic
  activity Semantic Kernel emits under that namespace.
- **Console exporter as a teaching tool**: `AddConsoleExporter()` prints raw
  trace data straight to the terminal, standing in for a real backend like
  Jaeger, Application Insights, or Datadog.
- **Spans and durations**: each captured `Activity` represents one unit of
  work (an LLM call or a plugin execution) with a measurable `Duration`,
  showing exactly where time goes in an agentic request.
- **Provider-agnostic instrumentation**: because OTel listens to SK's own
  instrumentation rather than anything provider-specific, the same tracing
  setup would work unchanged with any other connector.
- **Flushing buffered traces**: `tracerProvider.ForceFlush()` guarantees all
  recorded spans are written out before a short-lived console process exits.

## Cleanup notes

- Wrapped the core `kernel.InvokePromptAsync(...)` call in
  `try/catch/finally` (`Telemetry/Program.cs`). Previously an unhandled
  exception from the model/tool call (a dropped connection, a rejected API
  key, etc.) would crash the whole session *and* skip
  `tracerProvider.ForceFlush()`, meaning a failure produced no trace output
  at all - directly undermining this lesson's own point, since the trace of
  a failure is often the most instructive one. The fix keeps the happy path
  unchanged, prints a one-line `[ERROR]` message on failure, and always
  flushes traces in a `finally` block.
- The docx's "Complete Code" listing for `Program.cs` was updated to match
  and verified line-for-line against the source file.
- Everything else in `missing.md` (metrics/logs pillars, custom
  `ActivitySource` spans, sampling guidance, explicit plugin naming,
  `Microsoft.Extensions.Configuration` for the API key/model name, trace/log
  correlation, a dedicated error-path demo run) is a legitimate enhancement
  for a deeper observability lesson, but none of it blocks or confuses a
  student following this tutorial as written, so it was deliberately left
  alone rather than treated as a defect.
