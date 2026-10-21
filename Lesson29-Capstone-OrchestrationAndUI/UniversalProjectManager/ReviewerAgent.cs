using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;

namespace UniversalProjectManager
{
    /// <summary>Inspects every completed task and either approves it or sends it back to the Developer with feedback.</summary>
    internal class ReviewerAgent : IProjectAgent
    {
        private readonly Kernel _baseKernel;

        /// <inheritdoc/>
        public string Name => "Reviewer";

        /// <summary>Creates a reviewer that clones the given base kernel to inspect completed tasks.</summary>
        /// <param name="baseKernel">The shared kernel to clone AI service registrations from.</param>
        public ReviewerAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;
        }

        /// <summary>Reviews every completed task; on rejection, marks it incomplete again and appends the reviewer's feedback.</summary>
        /// <param name="state">The shared project state to read completed tasks from and write review outcomes back to.</param>
        public async Task ExecuteAsync(ProjectState state)
        {
            // Only review tasks that are marked completed by the developer
            var completedTasks = state.Tasks.Where(t => t.IsCompleted).ToList();

            if (!completedTasks.Any())
            {
                return;
            }

            Kernel isolatedKernel = _baseKernel.Clone();
            var settings = new OpenAIPromptExecutionSettings {  Temperature = 0.0 };

            foreach (var task in completedTasks)
            {
                Console.WriteLine($"  [REVIEWER] Inspecting Task: {task.Id}");

                string prompt = $@"
                    You are a string QA Reviewer.
                    Task: {task.Description}
                    Code Provided: {task.Result}

                    Deos the code completely satisfy the task?
                    If yes, output exactly: APPROVED
                    If no, output a 1-sentence explanation of what is wrong.";

                string review;
                try
                {
                    var result = await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
                    review = result.ToString().Trim();
                }
                catch (Exception ex)
                {
                    // A network hiccup, rate limit, or content filter here would otherwise crash
                    // the whole console session. Report it and leave this task's completion status
                    // untouched instead - it will simply be reviewed again next cycle.
                    Console.WriteLine($"  [REVIEWER] Failed to review task {task.Id}: {ex.Message}");
                    continue;
                }

                if (review.Contains("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  [REVIEWER] Task {task.Id} approved.");
                }
                else
                {
                    // Step: interpolate the log line so the real task ID and rejection reason are printed
                    Console.WriteLine($"  [REVIEWER] Task {task.Id} rejected. Reason: {review}");
                    // Mutate state to force re-work. Append the feedback instead of overwriting so the
                    // Developer's retry prompt still contains the original task description.
                    task.IsCompleted = false;
                    task.Description += $" [REVIEWER FEEDBACK] {review}";
                }
            }
        }
    }
}
