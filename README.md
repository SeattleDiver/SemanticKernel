# Applied Agentic Orchestration with Microsoft Semantic Kernel

Source code and written lesson material for a day-by-day video tutorial series on building agentic AI systems in C# with [Microsoft Semantic Kernel](https://github.com/microsoft/semantic-kernel). The series starts with a single LLM call and works up to multi-agent orchestration, with a capstone at the end of each part.

Every lesson is standalone. Each `DayN-*` folder holds its own Visual Studio solution and a Markdown lesson write-up, so you can open any day without building the earlier ones first.

## Requirements

| | |
|---|---|
| Runtime | [.NET 10 SDK](https://dotnet.microsoft.com/download) |
| IDE | Visual Studio 2026 (solutions use the `.slnx` format), or `dotnet` CLI / VS Code |
| Framework | `Microsoft.SemanticKernel` 1.78.0 with `Connectors.OpenAI` |
| Chat model | `gpt-4.1-mini` (Day 5 uses `gpt-4o-mini-search-preview` for web-grounded answers) |
| Embedding model | `text-embedding-3-small` (memory and RAG lessons) |

## Setup

All lessons read the OpenAI key from an environment variable. Keys are never stored in source.

```powershell
# Windows (PowerShell): persists for new terminals and Visual Studio
setx OPENAI_API_KEY "sk-..."
```

```bash
# macOS / Linux
export OPENAI_API_KEY="sk-..."
```

Day 8a (Cloud Librarian) also needs `PINECONE_API_KEY`.

Then open a lesson's `.slnx` in Visual Studio and press F5, or run it from the command line:

```bash
cd Lesson01-Conversationalist/Conversationalist
dotnet run
```

## Lesson structure

Each lesson document follows the same template: **Overview, Prerequisites, Setup, Core Concepts, Full Walkthrough / Code, Explanation, Expected Result**. Most lessons from Days 1–30 also include a `.docx` copy of the lesson inside the lesson folder.

## Part 1: Foundations & Core Orchestration (Days 1–30)

Single-agent basics (personas, tool calling, RAG) growing into multi-agent orchestration.

| Day | Lesson | Solution |
|---|---|---|
| 1 | [The Conversationalist](Lesson01-Conversationalist/project.md) | [Conversationalist.slnx](Lesson01-Conversationalist/Conversationalist.slnx) |
| 2 | [The Summarizer](Lesson02-Summarizer/project.md) | [Summarizer.slnx](Lesson02-Summarizer/Summarizer/Summarizer.slnx) |
| 3 | [The Format Translator](Lesson03-FormatTranslator/project.md) | [Translator.slnx](Lesson03-FormatTranslator/Translator/Translator.slnx) |
| 4 | [The Calculator: native functions & tool calling](Lesson04-Calculator/project.md) | [Calculator.slnx](Lesson04-Calculator/Calculator/Calculator.slnx) |
| 5 | [The Researcher: native model grounding](Lesson05-Researcher/project.md) | [Researcher.slnx](Lesson05-Researcher/Researcher/Researcher.slnx) |
| 6 | [The File Manager: OS agent](Lesson06-FileManager/project.md) | [FileManager.slnx](Lesson06-FileManager/FileManager/FileManager.slnx) |
| 7 | [The Data Analyst](Lesson07-DataAnalyst/project.md) | [DataAnalyst.slnx](Lesson07-DataAnalyst/DataAnalyst/DataAnalyst.slnx) |
| 8 | [Basic RAG Agent](Lesson08-RagAgent/project.md) | [RagAgent.slnx](Lesson08-RagAgent/RagAgent/RagAgent.slnx) |
| 8a | [The Cloud Librarian](Lesson08a-CloudLibrarian/project.md) | [CloudLibrarian.slnx](Lesson08a-CloudLibrarian/CloudLibrarian/CloudLibrarian.slnx) |
| 9 | [The Interviewer](Lesson09-Interviewer/project.md) | [Interviewer.slnx](Lesson09-Interviewer/Interviewer/Interviewer.slnx) |
| 10 | [The Code Generator](Lesson10-CodeGenerator/project.md) | [CodeGenerator.slnx](Lesson10-CodeGenerator/CodeGenerator/CodeGenerator.slnx) |
| 11 | [The Critic](Lesson11-Critic/project.md) | [Critic.slnx](Lesson11-Critic/Critic/Critic.slnx) |
| 12 | [The Visionary](Lesson12-Visionary/project.md) | [Visionary.slnx](Lesson12-Visionary/Visionary/Visionary.slnx) |
| 13 | [Prompt Logic Plugins](Lesson13-PromptLogicPlugins/project.md) | [PromptLogicPlugins.slnx](Lesson13-PromptLogicPlugins/PromptLogicPlugins/PromptLogicPlugins.slnx) |
| 14 | [Document QA](Lesson14-DocumentQA/project.md) | [DocumentQA.slnx](Lesson14-DocumentQA/DocumentQA/DocumentQA.slnx) |
| 15 | [Multi-Tool Agent](Lesson15-MultiToolAgent/project.md) | [MultiToolAgent.slnx](Lesson15-MultiToolAgent/MultiToolAgent/MultiToolAgent.slnx) |
| 16 | [Manual ReAct Loop](Lesson16-ManualReActLoop/project.md) | [ManualReActLoop.slnx](Lesson16-ManualReActLoop/ManualReActLoop/ManualReActLoop.slnx) |
| 17 | [Agent Framework (`ChatCompletionAgent`)](Lesson17-AgentFramework/project.md) | [AgentFramework.slnx](Lesson17-AgentFramework/AgentFramework/AgentFramework.slnx) |
| 18 | [Native Multi-Agent Orchestration](Lesson18-MultiAgent/project.md) | [MultiAgent.slnx](Lesson18-MultiAgent/MultiAgent/MultiAgent.slnx) |
| 19 | [CoordinatorPro](Lesson19-CoordinatorPro/project.md) | [CoordinatorPro.slnx](Lesson19-CoordinatorPro/CoordinatorPro/CoordinatorPro.slnx) |
| 20 | [Human-in-the-Loop](Lesson20-HumanInTheLoop/project.md) | [HumanInTheLoop.slnx](Lesson20-HumanInTheLoop/HumanInTheLoop/HumanInTheLoop.slnx) |
| 21 | [Semantic Routing](Lesson21-SemanticRouter/project.md) | [SemanticRouter.slnx](Lesson21-SemanticRouter/SemanticRouter/SemanticRouter.slnx) |
| 22 | [Prompt and Function Filters](Lesson22-FunctionFilters/project.md) | [FunctionFilters.slnx](Lesson22-FunctionFilters/FunctionFilters/FunctionFilters.slnx) |
| 23 | [Advanced RAG: hybrid search](Lesson23-AdvancedRAG/project.md) | [AdvancedRAG.slnx](Lesson23-AdvancedRAG/AdvancedRAG/AdvancedRAG.slnx) |
| 24 | [Telemetry & Observability](Lesson24-Telemetry/project.md) | [Telemetry.slnx](Lesson24-Telemetry/Telemetry/Telemetry.slnx) |
| 25 | [Long-Term Memory](Lesson25-LongTermMemory/project.md) | [LongTermMemory.slnx](Lesson25-LongTermMemory/LongTermMemory/LongTermMemory.slnx) |
| 26 | [Evaluator](Lesson26-Evaluator/project.md) | [Evaluator.slnx](Lesson26-Evaluator/Evaluator/Evaluator.slnx) |
| 27 | [Capstone I: design & architecture](Lesson27-Capstone/project.md) | [UniversalProjectManager.slnx](Lesson27-Capstone/UniversalProjectManager/UniversalProjectManager.slnx) |
| 28 | [Capstone I: plugins & single agents](Lesson28-Capstone-Plugins/project.md) | [UniversalProjectManager.slnx](Lesson28-Capstone-Plugins/UniversalProjectManager/UniversalProjectManager.slnx) |
| 29 | [Capstone I: orchestration & UI](Lesson29-Capstone-OrchestrationAndUI/project.md) | [UniversalProjectManager.slnx](Lesson29-Capstone-OrchestrationAndUI/UniversalProjectManager/UniversalProjectManager.slnx) |
| 30 | [Capstone I: deployment & review (Web API)](Lesson30-Capstone-DeploymentAndReview/project.md) | [UniversalProjectManager.slnx](Lesson30-Capstone-DeploymentAndReview/UniversalProjectManager/UniversalProjectManager.slnx) |

## Part 2: Closing the Orchestration-Pattern Gaps (Days 31–43, in progress)

Each lesson closes one specific gap found by auditing Part 1 against common agentic orchestration patterns.

| Day | Lesson | Solution |
|---|---|---|
| 31 | [Self-Reflection Loop](Lesson31-SelfReflectionLoop/Lesson.md) | [SelfReflectionLoop.slnx](Lesson31-SelfReflectionLoop/SelfReflectionLoop/SelfReflectionLoop.slnx) |
| 32 | [LLM-Based Fan-In](Lesson32-LLMFanIn/Lesson.md) | [LLMFanIn.slnx](Lesson32-LLMFanIn/LLMFanIn/LLMFanIn.slnx) |
| 33 | [Voting & Self-Consistency Ensembles](Lesson33-VotingEnsembles/Lesson.md) | [VotingEnsembles.slnx](Lesson33-VotingEnsembles/VotingEnsembles/VotingEnsembles.slnx) |
| 34 | [Multi-Agent Group Chat / Debate](Lesson34-GroupChatDebate/Lesson.md) | [GroupChatDebate.slnx](Lesson34-GroupChatDebate/GroupChatDebate/GroupChatDebate.slnx) |
| 35 | [Negotiation & Market-Based Coordination](Lesson35-MarketNegotiation/Lesson.md) | [MarketNegotiation.slnx](Lesson35-MarketNegotiation/MarketNegotiation/MarketNegotiation.slnx) |
| 36 | [Closed-Loop Plan-and-Execute](Lesson36-PlanAndExecute/Lesson.md) | [PlanAndExecute.slnx](Lesson36-PlanAndExecute/PlanAndExecute/PlanAndExecute.slnx) |
| 37 | [Safety Guardrail Middleware](Lesson37-GuardrailMiddleware/Lesson.md) | [GuardrailMiddleware.slnx](Lesson37-GuardrailMiddleware/GuardrailMiddleware/GuardrailMiddleware.slnx) |
| 38 | [Vector-Indexed Episodic Memory](Lesson38-EpisodicMemory/Lesson.md) | [EpisodicMemory.slnx](Lesson38-EpisodicMemory/EpisodicMemory/EpisodicMemory.slnx) |
| 39 | Multi-Level Hierarchical Delegation | *coming soon* |
| 40 | Event-Driven / Async Pub-Sub Agents | *coming soon* |
| 41 | Async Human-in-the-Loop | *coming soon* |
| 42 | Durable / Checkpointed Orchestration | *coming soon* |
| 43 | Capstone II: Distributed Resilient Multi-Agent System | *coming soon* |

The full syllabus is in [docs/syllabus.md](docs/syllabus.md).

## A note on experimental APIs

Semantic Kernel's `Agents.*` and `Process.*` namespaces are still experimental and can change between releases. Where one of those surfaces proved unreliable in live testing, the lesson says so and builds the same pattern by hand with plain `Kernel` / `IChatCompletionService` calls. Day 34 is a worked example of this.

## License

[MIT](LICENSE) © 2026 Media Six LLC
