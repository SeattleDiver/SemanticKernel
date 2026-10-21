// ------------------------------------------------------------------------------------------------
// Day 30: Refinement, Deployment & Review
// Project Overview
// Day 30 closes the capstone arc: the same GoalRefiner -> Planner -> Developer -> Reviewer
// workflow from Day 29, deployed both here (as a console app) and, side by side, as an
// ASP.NET Core Web API in the UniversalProjectManager.Api project.
// ------------------------------------------------------------------------------------------------

using Microsoft.SemanticKernel;

namespace UniversalProjectManager
{
    /// <summary>Entry point that builds the agent list and hands control to a ProjectOrchestrator for the whole workflow.</summary>
    internal class Program
    {
        /// <summary>Prompts for a project idea, then runs the full Refiner/Planner/Developer/Reviewer cycle via the orchestrator.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel baseKernel = builder.Build();

            Console.WriteLine("Universal Project Manager (UPM) - Phase 3 Orchestration");

            // Initialize Shared State
            ProjectState projectState = new ProjectState();

            Console.Write("Enter a project idea: ");
            string originalRequest = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(originalRequest)) return;

            projectState.OriginalRequest = originalRequest;

            // Initialize Agents
            IProjectAgent refiner = new GoalRefinerAgent(baseKernel);
            IProjectAgent planner = new PlannerAgent(baseKernel);
            IProjectAgent developer = new DeveloperAgent(baseKernel);
            IProjectAgent reviewer = new ReviewerAgent(baseKernel);
            List<IProjectAgent> agents = new List<IProjectAgent> { refiner, planner, developer, reviewer };

            // Pass the list of agents to the orchestrator
            ProjectOrchestrator orchestrator = new ProjectOrchestrator(agents);

            try
            {
                // Step: guard the run so a failed OpenAI call doesn't crash the whole console session
                await orchestrator.RunProjectAsync(projectState);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nOrchestration failed: {ex.Message}");
                return;
            }

            // Review the final state
            Console.WriteLine("\n--- FINAL PROJECT STATE ---");
            foreach(var task in projectState.Tasks)
            {
                Console.WriteLine($"\nTask: {task.Description}");
                Console.WriteLine($"Code:\n{task.Result}");
            }
            Console.WriteLine("---------------------------");

        }
    }
}
