# SemanticKernelSyllabus.md

# Course Syllabus: Becoming an Expert in Agentic AI with Microsoft Semantic Kernel

**Course Duration:** 31 Lessons, numbered Day 1–30 (including Day 8a)  
**Language:** C# / .NET 10  
**Primary AI Models:** Google Gemini (`gemini-2.5-flash`), `gemini-embedding-001`  
**Focus:** Building production-ready, autonomous, multi-agent workflows.

---

## 📖 Course Overview
Welcome to the Agentic AI Masterclass — 31 hands-on lessons, numbered Day 1 through Day 30 (including Day 8a). This course is designed to take you from basic LLM API calls to engineering enterprise-grade, autonomous AI systems. Using **Microsoft Semantic Kernel**, you will learn how to build AI agents that can reason, use tools, remember past interactions, collaborate with other agents, and safely interact with humans. 

We emphasize a **Production-First mindset**: focusing on strict typed outputs, secure architectural patterns, telemetry, and avoiding unstable experimental APIs in favor of deterministic, native orchestration.

## 🎯 Course Objectives
By the end of this course, you will be able to:
1. Construct specialized AI personas using System Prompts and Semantic Kernel.
2. Grant AI models the ability to execute local C# code autonomously via Plugins.
3. Architect complex multi-agent workflows that adhere to strict API protocols.
4. Implement advanced patterns like Hybrid RAG, Semantic Routing, and Human-in-the-Loop.
5. Apply enterprise standards including OpenTelemetry observability and Middleware filters.
6. Deploy a fully autonomous multi-agent system as a modern ASP.NET Core Web API with Scalar documentation.

## 🛠️ Prerequisites & Tech Stack
*   **Prerequisites:** Proficiency in C# and Object-Oriented Programming.
*   **Framework:** .NET 10 SDK
*   **Core Packages:** 
    *   `Microsoft.SemanticKernel`
    *   `Microsoft.SemanticKernel.Connectors.Google`
    *   `Microsoft.Extensions.AI`
    *   `Microsoft.SemanticKernel.Connectors.Pinecone` (Day 8a only)
*   **Tools:** Visual Studio 2026 (or VS Code + C# Dev Kit), Google Gemini API Key (`GEMINI_API_KEY`), and a free Pinecone API Key (`PINECONE_API_KEY`) for Day 8a only.
*   **Project layout:** each `DayN` folder is a standalone, independent project — there is no single solution file tying them together. Open the folder for the lesson you're on.

---

## 📅 Course Schedule

### Phase 1: Foundations & Single-Agent Personas (Days 1–14)
*Mastering the Semantic Kernel builder, prompt engineering, basic plugins, and data handling.*

*   **Day 1: The Conversationalist (Chatbot Agent)**
    *   *Concepts:* Kernel Builder, `ChatHistory` looping, `IChatCompletionService`.
*   **Day 2: The Summarizer (Reduction Agent)**
    *   *Concepts:* Prompt Templates, `KernelArguments`, `InvokePromptAsync`.
*   **Day 3: The Format Translator (Transformation Agent)**
    *   *Concepts:* Execution Settings, Zero-Hallucination Constraints, Temperature 0.0.
*   **Day 4: The Calculator (Math Agent)**
    *   *Concepts:* Native C# Plugins, `[KernelFunction]`, `ToolCallBehavior.AutoInvokeKernelFunctions`.
*   **Day 5: The Researcher (Web-Search Agent)**
    *   *Concepts:* Free REST API Integration (Wikipedia), `HttpClient`, White-Box Orchestration.
*   **Day 6: The File Manager (OS Agent)**
    *   *Concepts:* Side-Effects, System IO, Sandboxing Security.
*   **Day 7: The Data Analyst (Extraction Agent)**
    *   *Concepts:* Structured Outputs, `ResponseMimeType`, `ResponseSchema`, JSON Deserialization to C# Objects, LINQ Aggregation of Typed Results.
*   **Day 8: The Librarian (Basic RAG Agent)**
    *   *Concepts:* `Microsoft.Extensions.AI`, `IEmbeddingGenerator`, `gemini-embedding-001`, Cosine Similarity Math (computed by hand, no vector database).
*   **Day 8a: The Cloud Librarian (Pinecone Vector Database)**
    *   *Concepts:* `IVectorStore`, `Microsoft.Extensions.VectorData` Record Attributes (`VectorStoreKey`, `VectorStoreData`, `VectorStoreVector`), Cloud Vector Search via Pinecone.
*   **Day 9: The Interviewer (Context-Aware Agent)**
    *   *Concepts:* ChatHistory Management, System Message Steering, Stateful Iteration.
*   **Day 10: The Code Generator (Developer Agent)**
    *   *Concepts:* System Role Specialization, Markdown Formatting, Zero-Shot Coding.
*   **Day 11: The Critic (Evaluator Agent)**
    *   *Concepts:* The Rubric Pattern, Separation of Concerns, Structured Feedback.
*   **Day 12: The Visionary (Multimodal Agent)**
    *   *Concepts:* Multimodality, `ImageContent`, `ChatMessageContentItemCollection`.
*   **Day 13: The Dynamic Persona (Handlebars Agent)**
    *   *Concepts:* Handlebars Engine (`HandlebarsPromptTemplateFactory`), Conditional Logic in Prompts.
*   **Day 14: The Document QA (Large Context Agent)**
    *   *Concepts:* Context Windows vs. RAG, Instructional Anchoring, Streamlined Token Usage.

---

### Phase 2: Orchestration & Advanced Topics (Days 15–26)
*Moving from single-shot scripts to autonomous, reasoning systems and multi-agent coordination.*

*   **Day 15: Automatic Function Calling Integration**
    *   *Concepts:* Plugin Collections (The Utility Belt), Chaining Tool Calls, Auto-Invocation.
*   **Day 16: The Stepwise Planner (ReAct Pattern)**
    *   *Concepts:* The ReAct Loop (Thought -> Action -> Observation), Manual Agent Enumeration, Internal Monologues.
*   **Day 17: Semantic Kernel Agent Framework Basics**
    *   *Concepts:* `ChatCompletionAgent`, Agent Encapsulation.
*   **Day 18: Native Multi-Agent Orchestration (Stable Pattern)**
    *   *Concepts:* Persona Swapping, Shared `ChatHistory`, Google Gemini Protocol Alignment (`User -> Assistant` strict alternation).
*   **Day 19: The Coordinator (Advanced Orchestration)**
    *   *Concepts:* Structured JSON Routing (`ResponseMimeType`), Chain-of-Thought Meta-Agents, "Ghost" Nudges, HTTP Retry Resilience (`DelegatingHandler`).
*   **Day 20: Human-in-the-Loop (Approval Orchestration)**
    *   *Concepts:* Human Gatekeepers, Advisory Agents, Natural Protocol Alignment via user feedback.
*   **Day 21: Semantic Routing**
    *   *Concepts:* Intent Classification, Isolated Kernels via `.Clone()`, Security by Design.
*   **Day 22: Prompt and Function Filters**
    *   *Concepts:* Middleware logic, `IPromptRenderFilter`, `IFunctionInvocationFilter`, Global Logging.
*   **Day 23: Advanced RAG (Hybrid Search)**
    *   *Concepts:* Vector + Keyword Search, Context Merging, Deduplication (`.Union().Distinct()`).
*   **Day 24: Telemetry & Observability**
    *   *Concepts:* OpenTelemetry, Activity Sources, Span/Duration Measurement for performance profiling.
*   **Day 25: Long-Term Agentic Memory**
    *   *Concepts:* State Persistence (File I/O), Background Fact Extraction, Dynamic Context Injection.
*   **Day 26: Evaluating Agent Performance**
    *   *Concepts:* LLM-as-a-Judge, Groundedness Checking, Automated QA Test Suites.

---

### Phase 3: The Capstone Project (Days 27–30)
*Building the "Universal Project Manager (UPM)"—a cohesive, enterprise-ready C# solution built iteratively over four days.*

*   **Day 27: Capstone Design & Architecture**
    *   *Concepts:* The Blackboard Pattern (`ProjectState`), Interface-Driven Agents (`IProjectAgent`), Goal Refinement.
*   **Day 28: Capstone Implementation (Plugins & Single Agents)**
    *   *Concepts:* State-Aware Plugins, Planner Agent, Developer Agent, Cross-Agent Hand-offs.
*   **Day 29: Capstone Implementation (Orchestration & UI)**
    *   *Concepts:* Reviewer/QA Agent, The Orchestrator Loop, Automated Rework Cycles.
*   **Day 30: Refinement, Deployment & Review**
    *   *Concepts:* ASP.NET Core Web API, Classic MVC Controller Architecture (No Top-Level Statements), Transient Kernel Dependency Injection, Modern API documentation via **Scalar**.

---

## 🏆 Final Deliverable
By Day 30, students will possess a fully functional **Universal Project Manager API**. This system accepts a raw business requirement, autonomously spins up an AI management team to plan the architecture, writes the corresponding code, reviews the code for quality, and outputs a completed software specification to a modern web endpoint.