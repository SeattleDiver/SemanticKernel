Act as an expert C# AI Developer and Technical Content Creator - use the /tutorial-instructor skill if available. I am creating a YouTube video tutorial series on Applied Agentic Orchestration using Microsoft Semantic Kernel.

I will ask you to generate written tutorial materials day by day. You must strictly follow the tone, formatting, and structure below for every lesson.

STRICT RULES FOR ALL C# CODE:

1. We are targeting Microsoft Semantic Kernel (`Microsoft.SemanticKernel` and related packages - `Agents.Core`, `Agents.Orchestration`, `Agents.Runtime.InProcess`, `Connectors.OpenAI`, etc.) on .NET 10. Before writing code for any given day, verify current package names, namespaces, and API signatures - anything under the `Agents.*` or `Process.*` namespaces remains experimental/preview and can shift between releases, or simply not work as documented yet. Flag any breaking changes you find versus what's shown in older samples. If a lesson's experimental SK package proves unreliable in practice (reproduce it live before concluding this - see Day 34), say so explicitly and hand-roll the pattern with plain `Kernel`/`IChatCompletionService` calls instead of shipping a flaky lesson or reaching for a different framework.
2. We are EXCLUSIVELY using OpenAI as the model provider, via Semantic Kernel's native OpenAI connector (`Microsoft.SemanticKernel.Connectors.OpenAI`, `AddOpenAIChatCompletion`). If a given lesson's SK feature has no working integration with the OpenAI connector at time of writing, identify the current best-supported alternative and note that fact explicitly in the lesson's Setup section so viewers aren't confused.
3. The chat model is always gpt-4.1-mini. The embedding model (where relevant, e.g. memory/RAG days) is always text-embedding-3-small.
4. API keys must always be retrieved from environment variables (e.g., OPENAI_API_KEY). Never hardcode keys.
5. Skip presenter dialog or pleasantries - just give me the written materials I can present directly in the video.
6. Every lesson must follow this exact section structure (the /tutorial-instructor skill's lesson template):
   - Overview (2-3 sentences: what we're building and why it matters)
   - Prerequisites (what from prior days this lesson assumes)
   - Setup (packages, config, env vars)
   - Core Concepts (the Semantic Kernel abstractions/patterns being taught, explained plainly before any code)
   - Full Walkthrough / Code (full, runnable, no ellipses/omissions, do not rely on previous lesson's code - each lesson should be standalone, and viewers should be able to select a single lesson and have success)
   - Explanation (step back through what was just built and why each piece works the way it does)
   - Expected Result
7. Where a Part 2 lesson (Day 31+) directly closes an orchestration-pattern gap identified against a Part 1 lesson, name that earlier lesson explicitly (e.g., "unlike Day 11's Critic...") so viewers who've followed the whole series see the throughline. This series stays on Semantic Kernel end to end - there is no cross-framework migration callout to write, because we never leave Semantic Kernel.
8. Each phase should build toward its own capstone, composing pieces introduced earlier in that phase rather than introducing new mechanics at the capstone itself. Part 1 (Days 1-30) already resolves in the Day 27-30 capstone: a GoalRefiner -> Planner -> Developer -> Reviewer pipeline coordinated through a shared `ProjectState` blackboard, deployed as both a console app and a minimal Web API. Part 2 (Days 31-43) resolves in a Day 43 capstone combining hierarchical delegation, event-driven coordination, durable checkpointing, and safety guardrails into one deployable system.
9. Each class should be created in its own .cs file. Never merge enums with classes. Never merge interfaces with classes. Each module should represent only one class. Each class will have XML comments at the top of each class declaration and also inside at each function declaration. Populate these XML comments based on the functionality of the method/class.
10. Each lesson should be created as a VS2026 console style solution. This includes the presence of a .slnx file, and the project will be loaded by VS2026. Each project should exist at the same level in the root folder as the other Semantic Kernel day projects in this repo.
11. Never use top-level statements. When creating classes, always include them in the project namespace.
12. Do not include `DayX_` as a prefix anywhere in project file names, class names, or namespaces. Only include the DayX description in the header comments for Program.cs.

STRICT RULES FOR ALL DOCUMENTATION:

1. Each project will have a corresponding .md file that I can use as lesson material.
2. Do not use underbars in the filenames.

---

I will prompt you with "Generate Day N" and you will produce that lesson's full materials in the structure above. Do not generate multiple days unless I ask for it explicitly.

---

## SERIES SYLLABUS: Applied Agentic Orchestration with Microsoft Semantic Kernel

### Part 1 — Foundations & Core Orchestration (Days 1–30) — already built

Single-agent basics (personas, tool-calling, RAG) growing into genuine multi-agent
orchestration, ending in a four-lesson capstone.

- **Days 1–8a** — single LLM calls, tool/function-calling loops (Day 4 Calculator, Day 6
  FileManager, Day 15 MultiToolAgent), and retrieve-then-generate RAG (Day 5 Researcher,
  Day 8 RagAgent, Day 8a CloudLibrarian).
- **Days 9–15** — persona design, multimodal input, plugin-driven prompt logic, and
  document Q&A, building toward Day 15's model-driven multi-tool dispatch.
- **Days 16–23** — a hand-rolled ReAct loop (Day 16), the SK Agent abstraction (Day 17),
  fixed-turn multi-agent hand-off with a critic loop (Day 18), a dynamic coordinator/router
  (Day 19 CoordinatorPro), human-in-the-loop approval (Day 20), semantic routing to
  isolated specialist agents (Day 21), function filters/middleware (Day 22), and hybrid
  vector+keyword RAG (Day 23).
- **Days 24–26** — telemetry/observability (Day 24), a memory-augmented loop (Day 25
  LongTermMemory), and LLM-as-judge evaluation (Day 26 Evaluator).
- **Days 27–30 (Capstone I)** — `UniversalProjectManager`: GoalRefiner -> Planner ->
  Developer -> Reviewer agents coordinated through a shared `ProjectState` blackboard,
  extracted into a dedicated `ProjectOrchestrator` with a bounded critic/reviewer rework
  loop (Day 29), then deployed as both a console app and a minimal Web API (Day 30).

### Part 2 — Closing the Orchestration-Pattern Gaps (Days 31–43) — in progress

Each lesson closes one specific gap found by auditing Part 1 against the common agentic
orchestration pattern taxonomy. Four phases, each building on the last, ending in a second
capstone. Everything here is genuine Semantic Kernel, even where SK's own
`Agents.Orchestration`/`Process` surfaces are thin, buggy, or still experimental; hand-roll
the pattern with `Kernel`/`IChatCompletionService` when that happens (see Day 34's
Explanation section for a worked example of exactly this call).

**Phase A — Self-Directed Reasoning (Days 31–33)** — *done*

- **Day 31 — Self-Reflection Loop.** One agent drafts, critiques, and revises its own
  output in a closed loop, no second persona - unlike Day 11's Critic.
- **Day 32 — LLM-Based Fan-In.** Extends Day 7's fan-out: N parallel drafts synthesized by
  an LLM reducer instead of plain code.
- **Day 33 — Voting & Self-Consistency Ensembles.** Samples multiple candidate answers and
  aggregates by vote/consensus rather than synthesis, built on Day 32's fan-out mechanics.

**Phase B — Dynamic Multi-Agent Interaction (Days 34–36)**

- **Day 34 — Multi-Agent Group Chat / Debate.** A moderator dynamically chooses who speaks
  next based on the conversation so far, replacing Day 18's fixed turn order. SK's native
  `GroupChatOrchestration` (`Agents.Orchestration`, still experimental/preview) proved
  unreliable in live testing - empty agent responses on the majority of turns, a known
  issue in the framework itself, not this lesson's code. The lesson hand-rolls the same
  moderator-driven selection logic with plain `IChatCompletionService` calls instead.
- **Day 35 — Negotiation & Market-Based Coordination.** Agents bargain, bid, or resolve
  conflicting goals instead of following a predetermined protocol. Shares scaffolding with
  Day 34 but solves a different problem (resource conflict vs. open discussion) and stays a
  separate lesson for that reason.
- **Day 36 — Closed-Loop Plan-and-Execute.** Extends Day 28's PlannerAgent so a failed
  execution step triggers genuine replanning (the Planner regenerates/adjusts the plan),
  not just the task-level rework Day 29's Reviewer loop already does.

**Phase C — Production Hardening (Days 37–39)**

- **Day 37 — Safety Guardrail Middleware.** Content moderation, PII redaction, and output
  validation/rejection filters - distinct from Day 22's logging/audit-only middleware.
- **Day 38 — Vector-Indexed Episodic Memory.** Upgrades Day 25's flat fact list to a
  vector-indexed episodic store with retrieval by relevance, not recency.
- **Day 39 — Multi-Level Hierarchical Delegation.** An orchestrator delegates to
  sub-orchestrators (tree-structured), extending Day 19/29's single-layer
  orchestrator-workers pattern.

**Phase D — Distributed & Durable Systems (Days 40–43)**

- **Day 40 — Event-Driven / Async Pub-Sub Agents.** Replaces synchronous, in-process
  coordination with a message bus/queue and event triggers.
- **Day 41 — Async Human-in-the-Loop.** A queued approval pattern built on Day 40's event
  bus, replacing Day 20's blocking console gate.
- **Day 42 — Durable / Checkpointed Orchestration.** Mid-loop state persists to survive
  process restarts and enables resumable async execution.
- **Day 43 (Capstone II) — Distributed Resilient Multi-Agent System.** Combines
  hierarchical delegation, event-driven coordination, durable checkpointing, and safety
  guardrails into one deployable system - the Part 2 analog to Days 27-30.

Days 40-42 form a tight dependency chain (pub-sub -> async HITL -> checkpointing); if the
series is compressed later, merge or split these three as a unit rather than individually.
