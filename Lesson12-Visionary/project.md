# Day 12 — The Visionary

## What this project builds

A single-shot console program that sends a bundled sample chart image
(`Assets/sample-chart.png`) to OpenAI's gpt-4.1-mini alongside a text instruction,
and prints the model's description of the chart's type, key data points, and
overall trend. There's no conversation loop — one multimodal request goes
out, one answer comes back, and the program exits.

## Why it matters in the series

Every earlier lesson in the series sends the model plain text. This is the
first lesson to show that a single chat message can carry more than one
*kind* of content — text and image bytes together — and that Semantic
Kernel's `ChatHistory` API accepts that combined payload through the same
`AddUserMessage` call used for plain strings since Day 1. It's the bridge
between "the model can converse" and "the model can perceive," which later,
more advanced lessons build on.

## Core concepts taught

- **Multimodal chat messages**: bundling a `TextContent` instruction and an
  `ImageContent` (raw image bytes + MIME type) into one
  `ChatMessageContentItemCollection` so the model receives both in a single
  turn instead of two.
- **Provider-agnostic content abstractions**: `TextContent` and `ImageContent`
  are plain Semantic Kernel types; only the connector registration
  (`AddOpenAIChatCompletion`) is provider-specific.
- **Loading a build-time asset reliably at runtime**: using
  `AppContext.BaseDirectory` combined with the `.csproj`'s
  `CopyToOutputDirectory` setting so the image is found next to the compiled
  executable regardless of the process's working directory.
- **Task-appropriate temperature tuning**: a moderate `Temperature = 0.4` is
  chosen for descriptive/creative image analysis, in contrast to the
  near-zero settings used for deterministic, rule-following tasks in Days 10
  and 11.

## Cleanup notes

Two real defects were fixed because they would have crashed or confused a
student following the tutorial step by step:

1. **Missing existence check on the image path.** `File.ReadAllBytesAsync`
   was called directly on `Assets/sample-chart.png` with no
   `File.Exists` guard. If `bin`/`obj` were ever cleaned without a rebuild
   (or the DLL run from a different location), the program would crash with
   a raw `FileNotFoundException` instead of pointing the student at the
   `CopyToOutputDirectory` setting that's supposed to put the file there.
   Added a `File.Exists` check with a clear message before the read.
2. **Unhandled exception around the multimodal chat completion call.**
   `GetChatMessageContentAsync` was unguarded; since not all
   models/regions accept image input identically, an unsupported-content or
   safety-filter rejection would crash the whole session with a raw stack
   trace. Wrapped the call in a `try`/`catch` that prints a clear "the model
   rejected this image request" message instead.

While in there, a small null-guard was also added on `response.Content`
before printing it (matching the `if (nextQuestion.Content != null)` pattern
already used in Days 9/10), so a safety-filtered empty response prints an
informative line instead of nothing.

The following items from `missing.md` were deliberately left as enhancement
opportunities rather than defects, per this cleanup pass's scope: no support
for user-supplied images, no image size/format validation before upload, no
cancellation token support, no multi-turn follow-up Q&A about the image, and
no logging of image payload size/metadata. These are legitimate
production-hardening or feature ideas, but none of them block or mislead a
student running the lesson as written.
