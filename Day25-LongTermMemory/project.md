# Day 25 — Long-Term Memory

## What this project builds

A terminal-based personal assistant that remembers facts about the user
across process restarts. Unlike every prior lesson where `ChatHistory` lives
only in RAM and disappears the moment the console app exits, this project
gives the agent durable memory by persisting extracted facts to a plain
`user_memory.json` file on disk and reloading them the next time the app
starts.

On every turn the app runs two parallel "tracks":

- **`MemoryAgent`** — the foreground conversationalist. It rebuilds its
  system prompt each turn from whatever facts are currently known, so newly
  learned information is available to the model on the very next turn.
- **`MemoryManager`** — a background fact miner. It sends the user's raw
  input through a second, narrowly-scoped LLM call whose only job is to
  decide whether the message contains a durable personal fact (name,
  preferences, job, family, etc.) worth saving, or to answer `NONE`.

Anything worth keeping is appended to an in-memory `UserMemory` object (a
flat `List<string>` of facts) and immediately flushed to disk, so a fact
learned in one session is still known the next time the app is launched.

## Why it matters in the series

Earlier lessons treated "memory" as nothing more than an in-process
`ChatHistory`. This is the first lesson to introduce real persistence and to
demonstrate that "long-term memory" doesn't require a vector database or
external memory service — a JSON file and reliable read/write logic is
enough to make an agent feel like it remembers you. It also introduces the
pattern of reusing an LLM as a structured-output/classification tool (the
fact extractor) rather than only as a chatbot, and shows two independently
purposed classes sharing a single `IChatCompletionService`, which
foreshadows the multi-agent orchestration patterns used later in Phase 3.

## Core concepts taught

- **State persistence via file I/O** — `MemoryManager` serializes/deserializes
  a `UserMemory` object to/from `user_memory.json` with `System.Text.Json`,
  as a minimal, dependency-free substitute for a real memory store.
- **Background fact extraction as a classifier prompt** — `ExtractAndSaveFactAsync`
  uses a strict system prompt engineered to output either a short factual
  sentence or the literal string `"NONE"`, turning the LLM into a
  structured-output tool.
- **Dynamic, per-turn system prompt rebuilding** — `MemoryAgent.ChatAsync`
  reconstructs the system message from the live `UserMemory.Facts` list on
  every call, so memory updates are visible immediately without a restart.
- **Separation of concerns** — conversational responsibility (`MemoryAgent`)
  and persistence/extraction responsibility (`MemoryManager`) are split into
  two classes that both depend on the same shared `IChatCompletionService`.
- **Manual `ChatHistory` splicing** — the system/persona message is inserted
  at index 0 immediately before each call and removed immediately after, so
  the persistent history never accumulates stale system messages.
- **Provider message-role constraints** — a code comment documents a real
  Gemini restriction (a chat history can't contain only a system message),
  which is why the fact-extraction prompt is split into a system message and
  a separate user message.

## Cleanup notes

Two fixes were applied to `LongTermMemory/MemoryManager.cs` and
`LongTermMemory/Program.cs`, both of which could otherwise crash or mislead
a student stepping through this lesson:

- **`MemoryManager.LoadMemory()` could crash the whole app on startup.**
  It called `JsonSerializer.Deserialize<UserMemory>(json)` and returned the
  result directly, with no null fallback and no exception handling. An
  empty, truncated, or hand-edited `user_memory.json` (including a file left
  in a bad state by an earlier crash) would either return `null` — which
  then throws a `NullReferenceException` in `Program.cs` before the chat
  loop even starts — or throw an unhandled `JsonException`, either way
  killing the session on every subsequent run ("poisoning" the memory file
  permanently). Fixed by wrapping the read in a try/catch, falling back to a
  fresh `UserMemory` on any deserialization failure, and printing a warning
  instead of crashing.
- **Stale, actively misleading header comment in `Program.cs`.** The comment
  claimed the entry point was "still a placeholder" that "doesn't yet wire up
  a Kernel, chat loop, or MemoryManager instance," while the code immediately
  below fully builds the Kernel and runs the complete conversational loop.
  Corrected the comment to describe what the file actually does.

The docx's "Complete Code" listing for `Program.cs` and `MemoryManager.cs`
was updated to match both fixes.

Several other gaps flagged in the prior analysis (`missing.md`) were
deliberately left as enhancement opportunities rather than treated as
defects, since fixing them would mean production-hardening beyond this
lesson's teaching scope: no retry/backoff around the network-dependent LLM
calls, no `CancellationToken` plumbing, no fact deduplication/updating logic,
no pre-filter to skip the extraction call on trivial input, no discussion of
PII/privacy for the plaintext memory file, and no configurable/namespaced
memory file path.
