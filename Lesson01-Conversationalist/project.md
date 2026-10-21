# Day 1 — The Conversationalist

## What this project builds

A continuous, terminal-based chatbot. The program starts an infinite loop
that reads a line of text from the console, sends it to OpenAI's chat
model through Semantic Kernel, prints the reply, and waits for the next
line — until the user types `exit`.

## Why it matters in the series

This is the first lesson, so it's deliberately the smallest possible
end-to-end slice of Semantic Kernel: build a kernel, attach a model,
send one message, get one reply, and remember the conversation. Every
later day in the series builds on this same skeleton (`Kernel.CreateBuilder()`,
a connector-registration call, `IChatCompletionService`, and a
`ChatHistory`), so getting this pattern right here pays off for the
rest of the course.

## Core concepts it teaches

- **The Kernel Builder** — `Kernel.CreateBuilder()` is the factory used to
  configure and construct the `Kernel` before any AI calls can be made.
- **The Kernel** — the central registry of services, plugins, and
  configuration that the rest of an app's AI code draws on.
- **AddOpenAIChatCompletion** — the connector-registration call
  that attaches a specific model and API key to the builder, giving the
  otherwise "empty" kernel a model to talk to.
- **IChatCompletionService** — the abstraction Semantic Kernel uses to
  talk to chat-based LLMs. Code that depends on this interface instead
  of on OpenAI directly can swap providers by changing a single
  registration line.
- **ChatHistory** — an object that stores the ongoing conversation
  (including a system prompt) so the model has context of what was
  already said, since the underlying API calls are otherwise stateless.
- **The Conversation Loop** — a `while(true)` console loop that turns a
  single request/response call into a continuous, stateful chat
  experience.

## Cleanup notes

- **Fixed:** the call to `chatCompletionService.GetChatMessageContentAsync(...)`
  was previously unguarded. Any transient failure — a network blip, an
  invalid/expired API key, or an OpenAI rate-limit response — would throw
  an unhandled exception and crash the entire chat session, losing the
  whole conversation over a single bad turn. The call is now wrapped in
  a `try/catch` that prints a short, friendly message and lets the loop
  continue, so one failed turn doesn't end the session. This was the
  only change that met the "would block or confuse someone following
  the tutorial" bar; the `Program.cs` code sample in the docx was
  updated to match, and the build was re-verified after the change.
- **Left as enhancement opportunities (from `missing.md`), not defects:**
  retry/backoff and `CancellationToken` plumbing, a streaming
  (`GetStreamingChatMessageContentsAsync`) alternative, structured
  `ILogger` logging, configuration/secrets abstractions beyond the
  environment variable, chat-history/token-budget trimming, and pulling
  the model ID from an environment variable. These are all reasonable
  production-hardening ideas, but implementing them here would turn a
  deliberately minimal first lesson into something heavier than its
  teaching goal calls for.
