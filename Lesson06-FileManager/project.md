# Day 6 — The File Manager (OS Agent)

## What this project builds

A console app that gives a Semantic Kernel agent its first "hands." Every
earlier lesson only produced text (or, at most, did arithmetic); this one
introduces a `FileSystemPlugin` that can list, read, and write real files on
disk. The program seeds a dummy `server_logs.txt`, then hands the model a
deliberately vague, multi-step goal — "find the log file, figure out what
errors occurred, and write a summary to a new file" — without ever naming
the log file. The model has to autonomously chain `ListFiles` → `ReadFile` →
`WriteFile` through Semantic Kernel's auto tool-invocation to get there, and
the program proves the side effect is real by reading `error_summary.txt`
back off disk with plain C# I/O after the AI reports it's done.

## Why it matters in the series

This is the pivot point from "agent that talks" to "agent that acts." Once a
plugin can create, overwrite, or delete files, the cost of a mistake (by the
model or by a malicious prompt hiding inside a file it reads) goes from a
bad sentence to a damaged filesystem. Everything downstream in the course
that involves side effects builds on the sandboxing and multi-step tool
chaining introduced here.

## Core concepts it teaches

- **Side effects from an LLM-driven agent** — moving from "generate text"
  to "change the state of a real system."
- **`AddFromObject` vs. `AddFromType`** — `FileSystemPlugin` is registered
  as an already-constructed instance (`AddFromObject`) rather than letting
  Semantic Kernel construct it (`AddFromType<T>()`, as in Day 4), because
  its constructor does real setup work (capturing the sandbox directory)
  that must run exactly once, under the program's control.
- **AI sandboxing** — bounding what the model is allowed to touch on disk,
  rather than giving it free rein over the filesystem.
- **Sequential tool chaining via auto-invocation** — the model has to
  reason through an ordered sequence of tool calls (list, then read, then
  write) because it isn't told the log file's name up front.
- **Async native functions** — `ReadFileAsync`/`WriteFileAsync` show that
  `[KernelFunction]` methods can be `Task<string>`, not just synchronous.
- **External verification** — the program independently re-reads the
  output file from disk after the AI finishes, proving the side effect
  happened rather than trusting the AI's own report of success.

## Cleanup notes

The plugin's sandboxing was advertised (in both the code comments and the
docx) as bounding the AI to `Directory.GetCurrentDirectory()`, but the
actual implementation only used `Path.Combine(_currentDirectory, fileName)`
with no validation afterward — a `fileName` like `../../secrets.txt` (or an
absolute path) would have escaped the intended sandbox undetected, directly
contradicting this lesson's own stated security goal. Fixed by adding a
`TryResolveSandboxedPath` helper that resolves the combined path with
`Path.GetFullPath` and rejects anything that doesn't still start with the
sandbox root, used by both `ReadFileAsync` and `WriteFileAsync`.

Also wrapped the actual disk I/O in `ReadFileAsync`/`WriteFileAsync` in a
`catch` for `IOException`/`UnauthorizedAccessException`, returning a normal
tool-error string instead of letting a locked or permission-denied file
throw uncaught and crash the whole chat loop mid-chain.

Removed an unused `using System.Security.Cryptography.X509Certificates;`
directive left over from another file — harmless, but pointless clutter in
a file-manager demo.

The docx's "Complete Code" listing and the one "Code Walkthrough" sentence
that described the old, unguarded `Path.Combine` behavior were both updated
to match.

Left as deliberate enhancement opportunities, not defects (per the existing
`missing.md` analysis): no retry/backoff or error handling around the
`InvokePromptAsync` call itself, no overwrite protection on `WriteFile`, no
cap on how many tool round-trips the model can make, no handling of binary
or oversized files, no `ILogger`-based structured logging, and no unit
tests for the plugin's sandbox behavior.
