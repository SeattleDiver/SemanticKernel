# Introduction Video — Narrative Script

**Series:** Agentic AI with Microsoft Semantic Kernel (C# / .NET 10)
**Purpose:** Voice-over / on-camera script for the course introduction video. Pairs with `intro-slides.html`.

---

## Cold open (no slide, or title slide up)

Welcome. Over the next several lessons, you're going to build thirty-one AI agents from scratch — in C#, using Microsoft's Semantic Kernel — starting with a simple chatbot and ending with a multi-agent system that plans, writes, reviews, and ships its own software specifications through a live web API.

This video is just the setup. No coding here — by the end of it, you'll have the tools installed, an API key in hand, and a clear map of where the series is going.

---

## Section 1 — Setup: What You Need Installed

*(Slide: Setup)*

Three things, and all three are free.

**1. Visual Studio 2026.**
Go to `visualstudio.microsoft.com`, and download the **Community edition** — it's free for individual developers, students, and open-source work. When the installer opens, check the **".NET desktop development"** workload. That single checkbox brings in everything we need, including the **.NET 10 SDK** — this series targets .NET 10, which is a Long-Term-Support release Microsoft will support into November 2028, so you're building on stable ground.

If you'd rather use VS Code instead of full Visual Studio, that works too — install the **C# Dev Kit** extension and the standalone **.NET 10 SDK** from `dotnet.microsoft.com/download/dotnet/10.0`. Every lesson in this series runs equally well from either editor, or from the command line with `dotnet run`.

**2. A Google Gemini API key.**
This course uses Google's Gemini models as the LLM behind every agent we build. Getting a key takes about a minute:
- Go to `aistudio.google.com/apikey`.
- Sign in with a Google account.
- Click **"Create API key."**
- Copy the key — it starts with `AIza` — and treat it like a password. Don't paste it into code, don't commit it to source control.

The free tier requires no billing information and gives you real, usable quota on the Gemini Flash models we'll be using throughout the series.

**3. Set the key as an environment variable, once.**
Every single project in this repository reads the same environment variable — `GEMINI_API_KEY` — so you only need to set this up one time, and every lesson from Day 1 to Day 30 will pick it up automatically.

On Windows, open PowerShell and run:
```
setx GEMINI_API_KEY "your-key-here"
```
Then close and reopen your terminal (or Visual Studio) so it picks up the new variable.

On macOS or Linux, add this to your shell profile:
```
export GEMINI_API_KEY="your-key-here"
```

That's it. Tools installed, key generated, environment configured.

One lesson — Day 8a, the Cloud Librarian — also talks to Pinecone, a cloud vector database, and needs one additional free key (`PINECONE_API_KEY`) set the same way. That's covered in that lesson's own `project.md`, not here, since it's the only lesson in the series that needs it.

---

## Section 2 — Explanation: How This Repository Is Organized

*(Slide: Explanation)*

A quick map before we dive in, so nothing here surprises you.

**Every lesson is its own standalone project.** You'll see folders named `Day1-Conversationalist`, `Day2-Summarizer`, `Day3-FormatTranslator`, and so on, all the way through `Day30-Capstone-DeploymentAndReview`. There is intentionally no single giant solution file tying them all together — each day is a clean, self-contained console application (or, in the last lesson, a web API). When you're ready to work on a given day, open that day's folder — and just that folder — in Visual Studio 2026.

**Every lesson follows the same skeleton.** You'll build a `Kernel` using `Kernel.CreateBuilder()`, register a connection to Gemini with one line, and then layer that day's specific technique on top — a plugin, a planner, a filter, a second agent. Learn the skeleton once on Day 1, and every later lesson will feel familiar.

**Every lesson reads your API key the same way.** `Environment.GetEnvironmentVariable("GEMINI_API_KEY")` — no config files with secrets in them, nothing to change per-project. Set it once in Section 1, and it works for all 31 lessons (Day 8a additionally reads `PINECONE_API_KEY`, as noted above).

**Every folder has a `project.md`.** If you want a written summary of exactly what a given lesson builds and why — independent of this video — that file is your reference.

One honest note: the model behind every lesson is `gemini-2.5-flash`. It's fast, it's on the free tier, and it's consistent — so what you're learning is the *pattern*, not a specific model's quirks. If you swap in a different Gemini model later, the code doesn't change, only the string you pass to the connector.

---

## Section 3 — What You'll Learn

*(Slide: What You'll Learn)*

By the end of this series, you will be able to:
- Build specialized AI personas with system prompts and Semantic Kernel.
- Give an AI model the ability to run your C# code autonomously, through plugins.
- Architect multi-agent workflows where agents hand off work to one another.
- Implement production patterns: retrieval-augmented generation, semantic routing, human-in-the-loop approval, and telemetry.
- Deploy a finished multi-agent system as a real ASP.NET Core Web API.

We get there in three phases.

---

## Section 4 — Phase I: Foundations & Single-Agent Personas

*(Slide: Phase I)*

Phase One is Days 1 through 14 (fifteen lessons, counting Day 8a). This is where every core mechanic of Semantic Kernel gets introduced, one at a time, through a series of small, focused agents: a conversational chatbot, a summarizer, a format translator, a calculator that calls real C# functions, a researcher that hits a live web API, a file manager, a data analyst that forces the model into strict JSON so its output can be parsed and aggregated instead of just read, a basic retrieval-augmented-generation agent that computes its own vector similarity by hand, a cloud-backed version of that same agent running on a real Pinecone vector database, an interviewer that manages conversation state, a code generator, a critic that evaluates other output, a multimodal agent that reasons over images, a dynamic Handlebars-templated persona, and a document-QA agent that works over a large context window instead of retrieval.

By the end of Phase One, you won't just know Semantic Kernel's API surface — you'll understand *when* to reach for a plugin versus a prompt template versus raw context, because you'll have built one of each.

---

## Section 5 — Phase II: Orchestration & Advanced Topics

*(Slide: Phase II)*

Phase Two is Days 15 through 26, and this is where single agents become systems. You'll build a stepwise ReAct-style planner that reasons in a loop, wrap agents in the formal Agent Framework, and then wire two agents together into a real multi-agent conversation. From there, you'll build a Coordinator that dynamically decides which agent should speak next, add a human approval gate into that workflow, and add semantic routing so incoming requests get classified and sent to the right specialist automatically.

The back half of this phase is what makes a system production-grade rather than a demo: middleware-style prompt and function filters, hybrid vector-plus-keyword retrieval, OpenTelemetry instrumentation so you can actually see what your agents are doing, long-term memory that persists across sessions, and finally, a lesson on evaluating agent output automatically instead of eyeballing it.

---

## Section 6 — Phase III: The Capstone

*(Slide: Phase III)*

Phase Three is Days 27 through 30, and it's a single project built across four days: the **Universal Project Manager**. You hand it one plain-English business requirement, and it autonomously assembles a small AI team — a goal-refining agent, a planner, a developer agent, and a reviewer — that plans the work, writes the code, and reviews it for quality, looping back for rework automatically when the reviewer isn't satisfied.

Day 27 designs the shared state and agent interfaces. Day 28 builds out the plugins and the individual agents. Day 29 wires them together with a real orchestration loop. And Day 30 takes that exact same agent logic and deploys it — unchanged — behind a modern ASP.NET Core Web API with interactive Scalar documentation, so what started as a console experiment ends the series as a real, callable service.

---

## Section 7 — What We Learned

*(Slide: What We Learned — closing)*

Take a step back and look at the distance covered: from a `while` loop reading console input on Day 1, to an autonomous, multi-agent, self-reviewing system running behind a documented HTTP endpoint on Day 30 — using the exact same core building blocks the entire way. A Kernel. A connector. A prompt. A plugin. Chained together, filtered, routed, observed, and eventually served over the web.

That's the whole arc of this series: not thirty unrelated tricks, but one set of fundamentals, applied with increasing sophistication until it adds up to something genuinely production-shaped.

In the next video, we start on Day 1.

---

## Appendix — Notes for the Presenter (not for the video)

`docs/syllabus.md`, this script, and the slide deck have all been cross-checked against the actual code as of the addition of Day 7 and Day 8a — the series now has all 31 planned lessons built (Days 1–30, plus Day 8a). A couple of things still worth knowing before you record:

- **No root solution file:** there's no `.sln`/`.slnx` tying the 31 project folders together — that's by design (each day is independent), but it's worth saying out loud in Section 2 so viewers don't go looking for one.
- **Day 8a's Pinecone connector is a `-preview` package on a legacy API shape.** Pinecone itself archived its official .NET SDK repository, and the Semantic Kernel connector that wraps it (`Microsoft.SemanticKernel.Connectors.Pinecone`, pinned at `1.74.0-preview`) sits well behind this repo's `Microsoft.SemanticKernel` core version (1.78.0). It still builds and runs today — verified by compiling against the exact package versions before writing the lesson — but if you re-record this intro months from now, it's worth a quick `dotnet build` check on that one project before promising it works out of the box. Full detail is in `Day8a-CloudLibrarian/project.md`.
- **Attribute names moved.** If you (or a viewer) find older Semantic Kernel sample code for vector stores, it likely uses `VectorStoreRecordKey`/`VectorStoreRecordData`/`VectorStoreRecordVector`. The version pinned in this repo uses the shorter, renamed `VectorStoreKey`/`VectorStoreData`/`VectorStoreVector` instead — don't let older blog posts or docs samples confuse you if you go looking for more Pinecone connector examples.
