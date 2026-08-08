# Day 18 — Native Multi-Agent Orchestration

## What this project builds

A small console app that runs a two-agent "copywriting desk": a **Copywriter**
persona drafts a five-word product slogan, and an **Editor** persona reviews
it, either approving it or asking for one revision. The two agents go back and
forth over a shared conversation until the Editor says "APPROVED" or a
4-iteration safety cap is reached.

## Why it matters in the series

This is the first lesson to have two distinct AI "identities" collaborate on
the same task, but it deliberately avoids reaching for Semantic Kernel's
experimental multi-agent framework abstractions. Instead, it drives both
personas by hand through a single `ChatHistory`, so every hand-off — who
speaks next, what each agent sees, when the loop stops — stays visible in
plain C#. That makes it a natural bridge between the single-agent lessons
earlier in the series (e.g., Day 16's manual ReAct loop) and Day 19, which
takes this same two-agent pattern and formalizes it with a reusable
`CallAgent` helper and strict JSON output.

## Core concepts taught

- **Persona swapping** — `chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, persona))`
  right before a call, then `chatHistory.RemoveAt(0)` right after. The model
  briefly "becomes" whichever persona is inserted, without permanently
  polluting the shared transcript with system messages.
- **Shared memory as the hand-off mechanism** — both agents read and write to
  the *same* `ChatHistory` object, so the Editor sees exactly what the
  Copywriter wrote, and vice versa, with no explicit data passed between them.
- **Explicit hand-off cues in shared history** — the code inserts an extra
  `AddUserMessage(...)` "nudge" between agent turns so the next persona
  knows it's their turn to act. (The original Gemini version of this lesson
  needed this nudge to satisfy Gemini's strict User → Assistant alternation
  requirement; OpenAI's API has no such requirement, but the nudge earns
  its place anyway as the explicit "it's your turn" cue in shared history.)
- **Manual orchestration with a plain `while` loop** — there is no
  framework-level orchestrator object; the sequencing is 100% visible
  application code.
- **Bounded iteration as a termination guarantee** — `MaxIterations = 4`
  ensures the collaboration always ends, even if the Editor never approves.
- **String-based approval detection** — a simple `Contains("APPROVED", ...)`
  check, contrasted later (Day 19) with a move to structured JSON output.

## Cleanup notes

- **Fixed:** Both `chatService.GetChatMessageContentAsync` calls (Copywriter
  and Editor) ran with no exception handling. A transient OpenAI failure
  (rate limit, network blip, etc.) would throw an unhandled exception and
  crash the console app mid-collaboration. Each call is now wrapped in a
  minimal `try/catch` that prints a friendly `[ERROR]` message and breaks out
  of the loop cleanly instead of crashing the process. A `hadError` flag was
  added so the closing summary correctly reports "stopped early due to an
  error" instead of misleadingly saying "Max iterations reached."
- **Left as enhancement opportunities (not defects):** the fragile
  substring-based approval check, the duplicated persona-swap logic between
  the two agent turns, the lack of cancellation tokens/timeouts, the lack of
  a per-round console log, the lack of a distinct "final approved slogan"
  summary, and the minor wording typos in the persona strings. None of these
  block the tutorial from building or running as documented, and several
  (the duplication, the substring check) are explicitly revisited and
  improved in Day 19, so leaving them visible here preserves the intended
  before/after teaching contrast.
