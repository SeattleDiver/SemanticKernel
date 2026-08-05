# Day 11 — The Critic

## What this project builds

A single-shot "code critic" console app. A hard-coded C# snippet — a
`Calculate(int a, int b)` method with a deliberate unchecked-division bug —
is embedded inside a rubric-driven prompt and sent to Gemini 2.5 Flash. The
model is asked to score the snippet against three fixed criteria
(reliability, performance, best practices) and to return its verdict in a
strict `SCORE:` / `PROS:` / `CONS:` / `FIX:` text layout. The program prints
that critique to the console and exits — there's no conversation loop, no
follow-up turns, just one prompt in and one structured-looking answer out.

## Why it matters in the series

This lesson is a deliberate stepping stone toward real structured output.
Days 19 and 21 later introduce actual JSON-schema-enforced responses; Day 11
shows the "poor man's" version first — getting a consistent, parseable-*looking*
shape purely through careful prompt wording, with nothing in the SDK
enforcing that shape. Seeing the text-format approach (and its fragility)
before the JSON approach gives students a concrete before/after contrast
later in the course.

## Core concepts taught

- **Prompt-enforced structured output (text, not JSON)** — the exact
  `SCORE/PROS/CONS/FIX` layout is described in plain language inside the
  prompt string; the model complies because it was told to, not because
  anything validates it.
- **System/User roles embedded in one prompt string** — instead of building
  a `ChatHistory` with separate role objects, `System:` and `User:` labels
  are written directly into a single template string passed to
  `kernel.InvokePromptAsync`.
- **Temperature 0 for reproducible judgments** — `Temperature = 0.0, TopP =
  0.1` is used because a critic that scores the same code differently each
  run would undermine the whole exercise.
- **`KernelArguments` as a dual-purpose container** — one `KernelArguments`
  object carries both the `GeminiPromptExecutionSettings` and the `input`
  template variable.
- **`kernel.InvokePromptAsync` as a lighter-weight entry point** — unlike
  Days 9/10, this program never touches `IChatCompletionService` directly;
  it goes straight through the templated-prompt helper.

## Cleanup notes

- **Fixed:** wrapped the `kernel.InvokePromptAsync` call in a `try/catch`
  (`Program.cs`, Step 5). Previously any live-API failure — bad key, rate
  limit, transient network error, content-safety rejection — would throw an
  unhandled exception and crash the whole console session with a raw stack
  trace. This is exactly the kind of failure a student is likely to hit
  during class (shared/expired keys, flaky connections), so catching it and
  printing a clear "Critique failed - see error below" message keeps the
  lesson demoable. The docx's "Complete Code" listing was updated to match
  line-for-line.
- **Left as enhancement opportunities, not defects** (per `missing.md`):
  the lack of real parsing of the `SCORE/PROS/CONS/FIX` fields, the
  hard-coded (rather than file-read) review target, absent input validation
  on `codeToReview`, no `CancellationToken` support, minor comment typos
  ("witih", "Performane", "hightlights"), the lowercase `critic` namespace,
  and the single-sample (vs. multi-sample) demonstration. None of these
  block the tutorial from building, running, or being followed as written,
  so they were intentionally not touched.
- `dotnet build` succeeds with 0 warnings / 0 errors after the fix.
