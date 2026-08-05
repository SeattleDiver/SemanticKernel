# Day 3 — The Format Translator

## What this project builds

A small console app that acts as a "Transformation Agent": it takes a single
messy, human-written sentence describing a new hire and asks Gemini to
convert it into a strict JSON object with a fixed set of keys (`FirstName`,
`LastName`, `Age`, `JobTitle`, `Email`). The program prints the raw JSON
string the model returns to the console — there's no downstream parser in
this lesson, just the transformation step itself.

## Why it matters in the series

Day 1 and Day 2 showed a kernel doing open-ended chat and one-shot
summarization. Day 3 turns the same one-shot `InvokePromptAsync` pattern
toward a much narrower, more mechanical job: producing output that a
*program*, not a human, will consume next. That shift — from "answer a
question" to "emit a payload another piece of software depends on" — is the
foundation for every later lesson that has an agent hand structured data to
another agent, a plugin, or a tool.

## Core concepts covered

- **Transformation prompts** — framing the model's role as a strict data
  parser ("You are a strict data transformation agent") rather than a
  conversational assistant.
- **Negative prompting** — explicitly telling the model what *not* to do
  (no markdown fences, no commentary, no explanations) to keep its output
  machine-readable.
- **`GeminiPromptExecutionSettings`** — configuring model behavior
  (`Temperature`, `TopP`) from C# code instead of prompt text.
- **Temperature and TopP** — `Temperature = 0.0` and `TopP = 0.1` push the
  model toward its single most probable next token, trading creativity for
  repeatability, which matters when the output feeds a strict parser.
- **`KernelArguments(executionSettings)`** — showing that a single
  `KernelArguments` object can carry both prompt variables (`input`) and
  per-call model settings together.

## Cleanup notes

- Wrapped the `kernel.InvokePromptAsync` call in `Program.cs` in a
  `try`/`catch` (mirroring the exact pattern already used in
  `Day2-Summarizer`). Previously an API failure (bad key, rate limit,
  transient network error) on the one and only model call in this program
  would crash the process with a raw stack trace instead of a readable
  message — a real risk for students running this against a live API key.
  The `Translator.csproj` build still succeeds after the change, and the
  "Complete Code" section of the docx was updated to match.
- Left several items from the prior gap analysis (`missing.md`) as
  deliberate enhancement opportunities rather than defects: no
  `JsonSerializer.Deserialize` validation of the model's output, no
  interactive/CLI input (the sentence is hard-coded), no structured-output
  schema enforcement beyond prompt prose, a few typos in
  student-facing strings/comments, and no demonstration contrasting
  low- vs. high-temperature runs. None of these block the lesson from
  building or running as documented, so they were left alone per the
  cleanup scope for this pass.
