# Day 2 — The Summarizer

## What this project builds

A small console app that generates a demo article about quantum computing
into `article.txt`, reads it back in, and hands the text to OpenAI through a
prompt template that asks for exactly three bullet points. Where Day 1 was an
open-ended chatbot, Day 2 turns the model into a single-purpose "Reduction
Agent": one input file in, one deterministic summary out, no conversation
loop.

## Why it matters in the series

This is the first lesson where the model is used as a *function* rather than
a chat partner. It sets up two building blocks nearly every later lesson
depends on: prompt templates with injected variables, and the one-shot
`InvokePromptAsync` call as an alternative to a `ChatHistory` loop. Later days
(execution settings, plugins, agents) build directly on the prompt-template
pattern introduced here.

## Core concepts taught

- **Prompt Templates** — a fixed instruction string with a `{{$variable}}`
  placeholder, so the same instructions can run against different data.
- **KernelArguments** — the dictionary-like bridge that maps a C# value to a
  template placeholder by name (`"articleContent"` -> `{{$articleContent}}`).
- **InvokePromptAsync** — compiles and runs a prompt string directly against
  the `Kernel`, without manually managing a `ChatHistory` or chat completion
  service.
- **File I/O as agent input** — reading a whole file's contents
  (`File.ReadAllTextAsync`) into a prompt, the simplest form of feeding
  external data to an LLM call.

## Cleanup notes

Fixed two small defects that would otherwise get copied forward into
students' own code and one gap where the core AI call had no failure path:

- Corrected a typo in the student-facing prompt string ("ingore" ->
  "ignore") so it isn't propagated into future prompt-template experiments.
- Renamed the `KernelArguments` variable from `aguments` to `arguments`.
- Wrapped the `InvokePromptAsync` call (Step 5) in a `try`/`catch` so a bad
  API key, rate limit, or network blip prints a readable message instead of
  crashing the whole program with a raw stack trace — this is the one place
  in the file where an unhandled exception would end the session outright.

Left as deliberate enhancement opportunities (from `missing.md`), not
defects: retry/validation of the model's bullet-point output, execution
settings/temperature control, token/length guardrails on large input files,
and overwrite protection for `article.txt`. These are useful follow-ups but
are production-hardening concerns outside what blocks a student from
completing this lesson as written.
