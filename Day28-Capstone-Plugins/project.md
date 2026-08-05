# Day 28 — Universal Project Manager: Plugins & Single Agents

## What this project builds

This is day 2 of the 4-day Universal Project Manager (UPM) capstone (Day27 -> Day28 -> Day29 -> Day30). Day 27 scaffolded the shape of the system — the `IProjectAgent` interface, the shared `ProjectState`/`ProjectTask` blackboard, and a single `GoalRefinerAgent`. Day 28 adds the "muscle": two new concrete agents, `PlannerAgent` and `DeveloperAgent`, plus the first Semantic Kernel plugin in the series, `ProjectManagementPlugin`. `Program.cs` now chains three agents — Refiner, Planner, Developer — in a fixed, hand-written sequence and prints the final state, including the generated code for every task.

## Why it matters in the series

This is the lesson where the model stops just *describing* what it would do and starts actually *doing* it. Up through Day 27, an LLM call in this series produces text that the C# code goes on to parse or display. Here, the Planner's LLM call is handed a `[KernelFunction]` tool (`CreateTask`) and permission to invoke it autonomously — so the model itself decides to call C# code that mutates the shared `ProjectState`. That's the core shift plugins represent: giving a model the ability to change application state, not just generate more text.

## Core concepts taught

- **State-aware plugins** — `ProjectManagementPlugin` takes the shared `ProjectState` in its constructor and exposes `CreateTask` as a `[KernelFunction]`. Because the plugin closes over the same state instance every agent reads from, a tool call from the model really does add to the one shared task list.
- **`[KernelFunction]` / `[Description]` metadata** — these attributes on the method and each parameter are the entire interface the model sees; Semantic Kernel serializes them into a tool schema the model reasons over in natural language.
- **Auto-invoked tool calling** — `GeminiToolCallBehavior.AutoInvokeKernelFunctions` on the Planner's execution settings is what lets Semantic Kernel detect a tool-call request, execute the real C# method, and feed the result back to the model, all inside one `InvokePromptAsync` call.
- **Kernel cloning for tool isolation** — both `PlannerAgent` and `DeveloperAgent` call `_baseKernel.Clone()` before use. The Planner attaches `ProjectManagementPlugin` to its clone only; the Developer's clone never gets it, so the code-writing agent structurally cannot invoke `CreateTask`, even by accident.
- **Agent hand-off via shared state (the blackboard pattern)** — the Planner writes `ProjectTask` entries tagged `AssignedTo = "Developer"`; the Developer discovers its own work later by filtering `state.Tasks` itself. Neither agent calls the other directly.
- **Sequential manual orchestration** — `Program.cs` still `await`s each agent in a straight, hardwired sequence rather than looping over a `List<IProjectAgent>`, an intentional stepping stone before a real orchestrator is introduced later in the capstone.

## Cleanup notes

The following defects were fixed because each could block or confuse someone following the lesson step by step:

- **`ProjectTask.Result` had no default value** (`public string Result { get; set; }`), inconsistent with every other string property on the class (`Description`, `AssignedTo` both default to `string.Empty`). Under the project's `<Nullable>enable</Nullable>` setting this is a real nullable-reference inconsistency. Fixed by defaulting it to `string.Empty`, matching Day 27's own copy of the same file.
- **No error handling around any of the three `InvokePromptAsync` calls** (`GoalRefinerAgent`, `PlannerAgent`, `DeveloperAgent`). A transient failure (rate limit, network hiccup, content filter) on any of them would crash the whole console session with a raw, unexplained stack trace, partway through a three-stage pipeline. Wrapped each call in a try/catch that reports which stage failed and degrades gracefully (Refiner leaves `RefinedGoal` at its default, Planner leaves `state.Tasks` empty — which the Developer already tolerates — and the Developer skips only the failing task, letting the rest of the loop continue).
- **`DeveloperAgent._baseKernel` was declared `public readonly`**, unlike the equivalent field on every other agent (`private readonly`). This let external callers reach in and grab the kernel directly, bypassing the `IProjectAgent` contract. Changed to `private readonly` to match its siblings.

The docx's "Complete Code" listing and two prose notes (the Prerequisites carry-over note and the Code Walkthrough's Step 4 remark about the public field) were updated to match; the build still succeeds with 0 warnings / 0 errors afterward.

Deliberately left as enhancement opportunities, not defects (per `missing.md`): no cap on how many tasks the Planner can create, tasks assigned to roles other than "Developer" being silently orphaned, the plugin doing console I/O directly instead of through an abstraction, no validation/case-normalization of the `assignedTo` argument, absence of unit tests, and the unused `using` directives in `PlannerAgent.cs`. These are production-hardening or polish items, not things that would stop a learner from building and running the lesson as documented.
