# Day 8a — The Cloud Librarian

## What this project builds

The same five-fact "company knowledge base" RAG pipeline from Day 8,
with the hand-rolled parts replaced by a real cloud vector database. The
knowledge base is embedded with `text-embedding-3-small` exactly as
before, but instead of holding the vectors in a `List<KnowledgeDocument>`
and computing cosine similarity in a for-loop, each fact is upserted
into a Pinecone serverless index through Semantic Kernel's
`Microsoft.Extensions.VectorData` abstraction. The user's question is
embedded the same way and handed to Pinecone's own
`collection.SearchAsync(...)`, which returns the closest match and its
similarity score. That match is stuffed into the same grounded prompt
template Day 8 used, so the two lessons are directly comparable.

## OpenAI migration note

`AddOpenAIEmbeddingGenerator` (like Day 8's) is marked experimental in
this SK version and fails the build with `SKEXP0010` unless suppressed;
`CloudLibrarian.csproj` now sets `<NoWarn>$(NoWarn);SKEXP0010</NoWarn>`.
More importantly, the vector dimension is a real breaking change, not a
cosmetic one: `gemini-embedding-001` defaulted to 3072 dimensions,
but `text-embedding-3-small` defaults to **1536**. The
`[VectorStoreVector(...)]` attribute on `KnowledgeRecord.Vector` is now
`1536` to match — leaving it at `3072` would either fail the upsert or
silently corrupt the index, since Pinecone indexes are created with a
fixed dimension the first time `EnsureCollectionExistsAsync` runs.

## Why it matters in the series

Day 8 was explicit that its manual embed-and-scan approach was a
stand-in for "what a real vector database automates." This lesson is
that automation: the same retrieval task, but backed by an index that
scales to millions of documents instead of a handful living in memory.
It's also the first lesson with two external cloud dependencies at
once (OpenAI for embeddings/chat, Pinecone for storage/search), which
is closer to what a production RAG system actually looks like.

## Core concepts taught

- **`IVectorStore` / `PineconeVectorStore`** — Semantic Kernel's
  provider-agnostic vector store abstraction, backed here by Pinecone.
  The same `KnowledgeRecord` class and calling code would work against
  a different connector (Qdrant, Azure AI Search, etc.) with only the
  store construction line changed.
- **Vector store record attributes** — `[VectorStoreKey]`,
  `[VectorStoreData]`, and `[VectorStoreVector(dimensions, ...)]` from
  `Microsoft.Extensions.VectorData`, which map a plain C# class onto the
  fields Pinecone actually stores (an id, filterable/returnable text,
  and the embedding).
- **`EnsureCollectionExistsAsync` / `UpsertAsync` / `SearchAsync`** — the
  provider-agnostic lifecycle for a vector collection: create it if
  missing, write records into it, and query it by vector similarity,
  replacing Day 8's `OrderByDescending(CalculateCosineSimilarity)`.
- **A second cloud credential alongside `OPENAI_API_KEY`** —
  `PINECONE_API_KEY`, read and validated the same defensive way, since
  this lesson now depends on two independent external services either
  of which can fail.

## Implementation notes

- **Package/API surface was verified by compiling, not assumed.** This
  connector's public surface has changed over time — most notably, the
  attribute names shipped by the current
  `Microsoft.Extensions.VectorData.Abstractions` (pulled in transitively
  at v10.1.0 by `Microsoft.SemanticKernel.Connectors.Pinecone`
  1.74.0-preview) are `VectorStoreKeyAttribute`,
  `VectorStoreDataAttribute`, and `VectorStoreVectorAttribute` — **not**
  the older `VectorStoreRecordKey`/`VectorStoreRecordData`/
  `VectorStoreRecordVector` names the original curriculum notes assumed.
  Every call in `Program.cs` (`EnsureCollectionExistsAsync`,
  `UpsertAsync`, `SearchAsync`, the `VectorStoreVector` constructor
  taking only a dimension count with `DistanceFunction` as a separate
  named property) was confirmed against the real package by building a
  throwaway probe project before writing this lesson's code, and the
  full lesson project itself builds clean with `dotnet build`.
- **The vector dimension is hardcoded to 1536** to match
  `text-embedding-3-small`'s default output size (it supports lower
  dimensions via the connector's optional `dimensions` parameter, but
  this lesson doesn't configure that, so it uses the default). If a
  future embedding model changes its default dimension, this constant
  needs to move with it.
- **Pinecone's own .NET SDK support is a real risk worth knowing about
  before you build on this further:** Pinecone archived their official
  `pinecone-dotnet-client` repository, and Microsoft's own connector
  documentation flags this directly. The connector used here
  (`Microsoft.SemanticKernel.Connectors.Pinecone` 1.74.0-preview) still
  builds and runs against the current SDK version, but it's a `-preview`
  package sitting well behind this repo's `Microsoft.SemanticKernel`
  core version (1.78.0), and there's no guarantee of further updates.
  If Pinecone connectivity becomes unreliable in a future SDK bump, the
  fix is swapping the vector store construction (an actively maintained
  connector such as Qdrant or Azure AI Search) — the `KnowledgeRecord`
  class and all retrieval logic downstream of `IVectorStore` would not
  need to change.
- **No index region/cloud configuration is set explicitly** —
  `EnsureCollectionExistsAsync()` is called with defaults. If it fails
  in your Pinecone project, check the dashboard for your account's
  default serverless cloud/region rather than assuming the code is
  wrong.
- Same scope boundaries as Day 8: no retry/backoff around the OpenAI or
  Pinecone calls beyond the top-level guard around collection creation,
  no caching of embeddings across runs, and a single hardcoded question
  instead of an interactive loop.
- **Known flake, not a bug in this lesson's code:** Pinecone is eventually
  consistent, so `SearchAsync` immediately after the upsert loop can
  occasionally return zero results if the index hasn't caught up yet. If
  a run prints "No matching documents were found," just re-run it.
