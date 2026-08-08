# Day 8 — Basic RAG Agent (Retrieval-Augmented Generation From First Principles)

## What this project builds

A minimal, hand-rolled Retrieval-Augmented Generation (RAG) pipeline with no
vector database involved. A small "company knowledge base" of five plain-text
facts is embedded into vectors using an OpenAI embedding model, the user's
question is embedded the same way, and cosine similarity is computed by hand
to find the single most relevant document. That document is stuffed into a
grounded prompt template and handed to the chat model, which is instructed to
answer only from the supplied context.

## OpenAI migration note

`AddOpenAIEmbeddingGenerator` (the direct analog of Gemini's
`AddGoogleAIEmbeddingGenerator`) is marked experimental in this SK version
and fails the build with `SKEXP0010` unless suppressed. `RagAgent.csproj`
now sets `<NoWarn>$(NoWarn);SKEXP0010</NoWarn>` to acknowledge and accept
this — the same experimental-surface caveat CLAUDE.md flags for
`Agents.*`/`Process.*` namespaces applies here too, just for embeddings.

## Why it matters in the series

Every earlier lesson attaches a single chat-completion service to the Kernel.
This is the first lesson to register *two* AI services on the same `Kernel` —
a chat model and an embedding model — and the first to introduce the idea of
semantic search. Doing the retrieval step manually (no vector store, no SDK
magic) makes the mechanics fully visible before later lessons swap in a real
vector database that automates the same steps.

## Core concepts taught

- **Two services, one Kernel** — `AddOpenAIChatCompletion` and
  `AddOpenAIEmbeddingGenerator` registered side by side, since RAG needs
  both a way to embed text and a way to generate answers.
- **`IEmbeddingGenerator<string, Embedding<float>>`** — the
  `Microsoft.Extensions.AI` abstraction (arriving transitively via the OpenAI
  connector package) used to turn any string into a `ReadOnlyMemory<float>`
  vector via `GenerateVectorAsync`.
- **Cosine similarity, implemented by hand** — `CalculateCosineSimilarity`
  computes the angle between two vectors with a plain for-loop, showing the
  math behind semantic search instead of hiding it inside a library call.
- **Manual retrieval pipeline** — embed the knowledge base once up front,
  embed the incoming question, then rank with `OrderByDescending(...)
  .FirstOrDefault()` — standing in for what a vector database index does
  automatically.
- **Grounded / context-constrained prompting** — a prompt template that
  explicitly restricts the model to the retrieved `{{$context}}` and
  instructs it to say "I don't have enough information" rather than
  hallucinate, the core technique RAG uses to reduce made-up answers.

## Cleanup notes

- Fixed a latent crash: `bestMatch` (the result of
  `knowledgeBase.OrderByDescending(...).FirstOrDefault()`) could be `null` if
  the knowledge base were ever empty, and the code immediately dereferenced
  `bestMatch.Text` a few lines later with no guard — a raw
  `NullReferenceException` instead of a clear message. Added a `Step 7b` null
  check right after the retrieval line that prints a friendly message and
  returns instead of crashing. This also cleared the compiler's `CS8602`
  possible-null-dereference warning.
- Left as deliberate enhancement opportunities (from `missing.md`), not
  defects: no try/catch or retry around the embedding/chat API calls, no
  persistence/caching of the computed embeddings, the hard-coded single
  question instead of an interactive loop, no similarity-score threshold or
  printed confidence score, no `CancellationToken` support, and the
  `KnowledgeDocument.Text` nullable-reference warning (`CS8618`) — none of
  these block the tutorial as written, so they were left for a future
  revision rather than fixed here.
- Verified the docx's package versions (`Microsoft.SemanticKernel` 1.78.0,
  `Microsoft.SemanticKernel.Connectors.OpenAI` 1.78.0) and extension
  method names (`AddOpenAIChatCompletion`, `AddOpenAIEmbeddingGenerator`)
  still match the `.csproj` and `Program.cs` exactly — no stale
  package/method references found.
