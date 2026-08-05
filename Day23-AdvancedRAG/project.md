# Day 23 — Advanced RAG (Hybrid Search)

## What this project builds

A small console app that seeds an in-memory "knowledge base" of three
sentences about a fictional server rack (the "Alpha-99"), embeds each one
with a Gemini embedding model, and then answers a single hardcoded question
— "What is the maintenance schedule for the Alpha-99" — by retrieving
relevant context and feeding it into a grounded, low-temperature Gemini
prompt. The pipeline is split across four files: `Document` (data),
`HybridRetriever` (retrieval), `RagAgent` (generation), and `Program.cs`
(wiring).

## Why it matters in the series

This lesson upgrades the naive, vector-only retrieval from Day 8's RAG
lesson into **Hybrid Search**. Pure vector similarity is excellent at
catching semantic meaning but can miss exact identifiers (part numbers,
product codes, names) that don't embed distinctively. Day 23 shows how to
run a lexical (keyword/substring) pass alongside the vector pass and merge
the two result sets — a pattern used heavily in production RAG systems
that need both "close in meaning" and "exact match" recall.

## Core concepts taught

- **Hybrid Search** — combining lexical (substring) and semantic (vector)
  search over the same document set so both exact-term queries and
  conceptual queries are handled well.
- **`Microsoft.Extensions.AI`'s `IEmbeddingGenerator`** — `HybridRetriever`
  and `Document` are written entirely against the standard
  `IEmbeddingGenerator<string, Embedding<float>>` interface rather than a
  proprietary Semantic Kernel type, decoupling retrieval logic from any
  specific provider.
- **Cosine similarity** — `HybridRetriever.CalculateCosineSimilarity`
  manually computes the dot product and vector norms to score how close a
  document's embedding is to the query's embedding.
- **Merging and deduplication** — `SearchAsync` combines the top
  vector-search hits and keyword-search hits with `.Union(...).Distinct()`
  so a document found by both methods is only sent to the model once.
- **Grounded, low-temperature prompting** — `RagAgent.AnswerAsync` sets
  `Temperature = 0.0` and instructs the model to say exactly
  `"Data not found"` when the answer isn't in the supplied context.
- **Decoupled pipeline components** — `Document`, `HybridRetriever`, and
  `RagAgent` are each independently testable and swappable; `Program.cs` is
  the only place that wires them together.

## Cleanup notes

- Fixed a one-character escape-sequence typo in `Program.cs`:
  `Console.WriteLine($"\bUser Query: {query}")` used `\b` (backspace)
  instead of the clearly-intended `\n` (every other `Console.WriteLine` in
  the file uses `\n` for spacing). This produced confusing or invisible
  output depending on the terminal instead of the intended blank line
  before "User Query:". Also synced the docx's "Complete Code" listing to
  match.
- Everything else flagged in the prior analysis (`missing.md`) — retry
  logic around embedding/chat calls, stopword filtering for the keyword
  pass, a similarity-score threshold on vector search, persistence of
  embeddings across runs, an interactive query loop, and surfacing
  `Document.Id` in retrieval results — was left in place deliberately.
  These are production-hardening or pedagogical enhancements, not defects
  blocking the tutorial as written; the demo builds and runs correctly
  without them.
