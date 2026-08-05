// ------------------------------------------------------------------------------------------------
// Day 28: Capstone Implementation(Plugins & Single Agents)
// Project Overview
// In Day 28, we implement the "Muscle" of the Universal Project Manager (UPM). Continuing in the
// single Capstone solution we started on Day 27, we will add two concrete agents implementing our
// IProjectAgent interface: the Planner Agent and the Developer Agent.
// ------------------------------------------------------------------------------------------------

using Microsoft.SemanticKernel;

namespace UniversalProjectManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
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
                // Step: guard the run so a failed Gemini call doesn't crash the whole console session
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
