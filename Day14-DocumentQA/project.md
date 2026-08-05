# Day 14 — Document QA

## What this project builds

A terminal-based question-answering tool that grounds every answer in a small
"technical manual" for a fictional device (the "Quantum-X"). Instead of
retrieving relevant snippets from a document store, the *entire* document is
pasted directly into the prompt on every turn — a brute-force technique
usually called "context stuffing." The student asks free-form questions in a
console loop; the model is instructed to answer only from the supplied text
and to say it doesn't know when the manual doesn't cover something.

## Why it matters in the series

This lesson is the deliberate foil to the retrieval-augmented generation
(RAG) lessons elsewhere in the course (Day 8, Day 23). It shows the simplest
possible way to get a model to answer from "your data" — no vector store, no
embeddings, no chunking — and lets students feel the ceiling of that approach
firsthand: it works great while the source document is small enough to fit
in a prompt, and stops scaling the moment it isn't. That contrast is what
makes the later RAG lessons land.

## Core concepts taught

- **Prompt templates with multiple named variables** — `promptTemplate` uses
  two `{{$...}}` placeholders (`documentContent` and `question`), showing
  that `KernelArguments` is a general-purpose dictionary of substitutions,
  not limited to one slot.
- **`kernel.InvokePromptAsync`** — invoking a raw prompt string directly
  against the kernel, bypassing `IChatCompletionService`/`ChatHistory`
  entirely, as the simplest path from template to model response.
- **Context stuffing as a grounding technique** — embedding the full source
  text inside `--- BEGIN MANUAL ---` / `--- END MANUAL ---` markers so the
  model has everything it needs with no retrieval step.
- **Grounding and refusal instructions** — telling the model to answer only
  from the supplied text and to admit when it doesn't know, a core
  prompt-engineering pattern for reducing hallucination.
- **`GeminiPromptExecutionSettings.Temperature = 0.0`** — using a
  deterministic, low-variance setting for factual extraction tasks.
- **Stateless Q&A loop** — each question is answered independently with no
  `ChatHistory`, in contrast to the memory-carrying chatbot from Day 1.

## Cleanup notes

- Fixed: the Gemini call inside the question loop (`kernel.InvokePromptAsync`)
  had no error handling. Any transient failure (network hiccup, rate limit,
  malformed response) would throw out of the `while` loop and crash the
  whole console session, ending the demo mid-class. Wrapped the call in a
  `try/catch` that prints the error and lets the loop continue so a single
  bad turn doesn't take down the program. Both `Program.cs` and the docx's
  Complete Code listing were updated to match; the build was verified with
  `dotnet build`.
- Left as enhancement opportunities (from `missing.md`), not defects: loading
  a real file from disk instead of the simulated document, a context-window
  size guardrail/warning, an explicit runtime note about the lack of
  cross-question memory, using `result.GetValue<string>()` instead of the
  implicit `ToString()`, packaging the document lookup as a `[KernelFunction]`
  plugin, and extracting the hard-coded persona/document into constants or
  config. These are production-hardening/forward-looking improvements, not
  things that block or confuse a student following the lesson as written.
