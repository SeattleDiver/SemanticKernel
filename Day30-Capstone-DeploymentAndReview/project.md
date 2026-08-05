# Day 30 — Refinement, Deployment & Review (Enterprise Web API)

## What this project builds

Day 30 is the closing lesson of the four-day Universal Project Manager (UPM)
capstone (Day 27 → 28 → 29 → 30). It takes the exact multi-agent workflow
built through Day 29 — GoalRefiner → Planner → Developer → Reviewer,
orchestrated in a rework loop by `ProjectOrchestrator` — and deploys it two
ways side by side:

- **`UniversalProjectManager`** — the original console app, carried forward
  unchanged in behavior. `Program.cs` builds one `Kernel`, constructs the four
  agents by hand, and runs a single project through the orchestrator per
  process execution.
- **`UniversalProjectManager.Api`** — a brand-new ASP.NET Core Web API
  (Controller-based, not Minimal APIs) that wraps the *same* agent classes
  behind a single HTTP endpoint, `POST /api/project/generate`. Instead of one
  `Kernel` built by hand in `Main`, the Web API registers the `Kernel`, every
  `IProjectAgent` implementation, and the `ProjectOrchestrator` itself as
  `AddTransient` services in the ASP.NET Core DI container, so the framework
  assembles a fresh agent team for every request. An interactive Scalar UI is
  served at `/scalar/v1` in place of the default `dotnet new webapi`
  `WeatherForecastController` sample.

## Why it matters in the series

This is the pivot from "agent code that runs in a console" to "agent code
that's a deployable service." The agent classes themselves are byte-for-byte
identical between the two projects (aside from `internal` → `public` access
modifiers, required so the Web API project can reference and DI-register
them). Nothing about the *agentic logic* changes — only how those classes get
constructed and wired together. That's the payoff of having designed the
agents around a plain interface (`IProjectAgent`) and constructor injection
(`Kernel baseKernel`) back in Day 27-28: the same business logic is trivially
portable into a completely different hosting model.

## Core concepts taught

- **Kernel registration via DI** — `builder.Services.AddTransient<Kernel>(sp => {...})`
  replaces the console app's one-time `Kernel.CreateBuilder().Build()` call
  with a factory the whole ASP.NET Core pipeline can inject anywhere.
- **Transient lifetime for request isolation** — every service (`Kernel`, all
  four agents, the orchestrator) is `AddTransient`, guaranteeing two
  concurrent HTTP requests never share a `Kernel` or agent instance.
- **`IEnumerable<IProjectAgent>` multi-registration** — registering four
  concrete types against the same interface lets the container automatically
  hand `ProjectOrchestrator`'s constructor a collection containing one of
  each, with no manual list-building.
- **Controller as a thin adapter** — `ProjectController` knows nothing about
  Gemini, prompts, or task lists; it validates input, builds a request-scoped
  `ProjectState`, calls the orchestrator, and returns JSON.
- **OpenAPI/Scalar documentation** — `AddOpenApi()` + `MapScalarApiReference()`
  give students a browsable, "try it out" UI instead of hand-written `curl`
  commands.
- **What deploying to a Web API does *not* fix** — the five-cycle safety cap,
  the Reviewer's rework loop, and every prompt string are untouched; moving to
  HTTP changes how a client talks to the system, not the underlying agentic
  behavior.

## Cleanup notes

Both project subfolders carried forward the same four defects (each existing
independently in both `UniversalProjectManager` and `UniversalProjectManager.Api`
copies of the affected files), so each fix below was applied twice:

- **`ProjectTask.Result` had no default**, unlike its sibling string
  properties (`Id`, `Description`, `AssignedTo`), which all default to
  `string.Empty`. With `<Nullable>enable</Nullable>` in both `.csproj` files
  this is a real nullable-reference inconsistency. Fixed by adding
  `= string.Empty` to match its siblings.
- **`ReviewerAgent.cs`'s rejection log line was missing its `$` prefix** —
  `Console.WriteLine("... {task.Id} ... {review}")` printed the literal
  placeholder text instead of interpolating, in both the console output and
  (now) the JSON-returning Web API. This bug had even been baked into the
  docx's own "Expected Output" transcript as if it were correct. Fixed by
  adding the missing `$`, and updated the docx's Complete Code and Expected
  Output sections to match.
- **No exception handling around the orchestration run** — in the console
  app, an unhandled exception from `RunProjectAsync` (e.g., a Gemini network
  error) would crash the whole session with a raw stack trace; in the Web
  API, the same exception would bubble up through `ProjectController` as an
  unhelpful generic HTTP 500. Added a `try/catch` around the orchestrator
  call in both `Program.cs` and `ProjectController.cs` — the console app now
  prints a clear failure message and exits gracefully, and the API now
  returns HTTP 502 with a descriptive message instead of an opaque 500.

**Deliberately left as enhancement opportunities** (per `missing.md`, not
treated as defects): the leftover `WeatherForecast.cs` template file with no
controller (harmless scaffold cruft); no authentication/authorization, no
request-DTO validation, no API versioning, no CORS configuration, and no
containerization/deployment artifacts on the Web API (all legitimate
"enterprise readiness" gaps, but production-hardening work beyond this
lesson's scope); and the long-running, non-cancellable, synchronous
request/job pattern. The `ReviewerAgent`'s behavior of *overwriting* (rather
than appending to) a rejected task's `Description` with the reviewer's
feedback was also left as-is — it's a debatable design choice rather than an
unambiguous defect, and correcting it would ripple through the docx's
Expected Output and example JSON response in ways disproportionate to a
one-line teaching fix.
