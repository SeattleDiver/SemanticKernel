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
    /// <summary>Entry point that chains GoalRefiner, Planner, and Developer agents through the shared ProjectState blackboard.</summary>
    internal class Program
    {
        /// <summary>Prompts for a rough project idea, then runs it through the Refiner, Planner, and Developer stages in sequence.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel baseKernel = builder.Build();

            Console.WriteLine("Universal Project Manager (UPM) - Phase 2 Implementation");

            // Initialize Shared State
            ProjectState projectState = new ProjectState();
            Console.Write("Enter a rough project idea:");
            
            string originalRequest = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(originalRequest)) return;

            projectState.OriginalRequest = originalRequest;

            // Initialize Agents
            IProjectAgent refiner = new GoalRefinerAgent(baseKernel);
            IProjectAgent planner = new PlannerAgent(baseKernel);
            IProjectAgent developer = new DeveloperAgent(baseKernel);

            // Sequential Exeuction (Manual Orchestration for now)
            await refiner.ExecuteAsync(projectState);
            Console.WriteLine($"Target Goal: {projectState.RefinedGoal}\n");

            Console.WriteLine($"[{planner.Name.ToUpper()}] is analyzing the goal and generating tasks...");
            await planner.ExecuteAsync(projectState);
            Console.WriteLine($"Planning complete. {projectState.Tasks.Count} tasks created.\n");

            Console.WriteLine($"[{developer.Name.ToUpper()}] is writing the code...");
            await developer.ExecuteAsync(projectState);

            // Review the final state
            Console.WriteLine("\n--- FINAL PROJECT STATE ---");
            foreach(var task in projectState.Tasks)
            {
                Console.WriteLine($"\nTask ID : {task.Id}");
                Console.WriteLine($"Desc    : {task.Description}");
                Console.WriteLine($"Status  : {(task.IsCompleted ? "DONE" : "PENDING")}");
                Console.WriteLine($"Code    :\n{task.Result}");
            }
            Console.WriteLine("---------------------------");

        }
    }
}
