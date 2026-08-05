using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace UniversalProjectManager
{
    /// <summary>
    /// Scans the ProjectState for assigned tasks and executes code generation.
    /// </summary>
    internal class DeveloperAgent : IProjectAgent
    {
        public readonly Kernel _baseKernel;
        public string Name => "Developer";

        public DeveloperAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;
        }

        public async Task ExecuteAsync(ProjectState state)
        {
            // Filter the shared state for tasks assigned to this agent that are pending
            var pendingTasks = state.Tasks.Where(t => t.AssignedTo == this.Name && !t.IsCompleted).ToList();

            if (!pendingTasks.Any())
            {
                Console.WriteLine("   [DEVELOPER] No pending tasks found for this agent.");
                return;
            }

            // Clone the Kernel to ensure thread safety and avoid shared state issues
            Kernel isolatedKernel = _baseKernel.Clone();
            var settings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.1
            };

            // Execute the tasks sequentially
            foreach(var task in pendingTasks)
            {
                Console.WriteLine($"   [DEVELOPER] Executing task: {task.Id} {task.Description}");
                string prompt = $@"
                    You are a Senior C# Developer.
                    Project Goal: {state.RefinedGoal}
                    Current Task: {task.Description}

                    Write only the C# code to fulfill this task.  Do not include markdown formatting or explanations.
                ";

                var result = await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));

                // Update the shared state
                task.Result = result.ToString().Trim();
                task.IsCompleted = true;
            }
        }
    }
}