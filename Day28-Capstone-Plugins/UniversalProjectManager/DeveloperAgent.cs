using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace UniversalProjectManager
{
    /// <summary>
    /// Scans the ProjectState for assigned tasks and executes code generation.
    /// </summary>
    internal class DeveloperAgent : IProjectAgent
    {
        // Step: private like every other agent's kernel field, so callers must go through
        // the IProjectAgent contract instead of reaching in and grabbing the kernel directly.
        private readonly Kernel _baseKernel;

        /// <inheritdoc/>
        public string Name => "Developer";

        /// <summary>Creates a developer agent that clones the given base kernel to write code for its assigned tasks.</summary>
        /// <param name="baseKernel">The shared kernel to clone AI service registrations from.</param>
        public DeveloperAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;
        }

        /// <summary>Writes code for every pending task assigned to this agent, marking each complete as it finishes.</summary>
        /// <param name="state">The shared project state to read pending tasks from and write results back to.</param>
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
            var settings = new OpenAIPromptExecutionSettings
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

                try
                {
                    var result = await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));

                    // Update the shared state
                    task.Result = result.ToString().Trim();
                    task.IsCompleted = true;
                }
                catch (Exception ex)
                {
                    // Step: a network hiccup, rate limit, or content filter here would otherwise crash
                    // the whole console session. Report it and leave this task pending instead, so the
                    // remaining tasks in the loop still get a chance to run.
                    Console.WriteLine($"   [DEVELOPER] Failed to complete task {task.Id}: {ex.Message}");
                }
            }
        }
    }
}