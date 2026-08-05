using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalProjectManager
{
    public class ReviewerAgent : IProjectAgent
    {
        private readonly Kernel _baseKernel;
        public string Name => "Reviewer";

        public ReviewerAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;
        }

        public async Task ExecuteAsync(ProjectState state)
        {
            // Only review tasks that are marked completed by the developer
            var completedTasks = state.Tasks.Where(t => t.IsCompleted).ToList();

            if (!completedTasks.Any())
            {
                return;
            }

            Kernel isolatedKernel = _baseKernel.Clone();
            var settings = new GeminiPromptExecutionSettings {  Temperature = 0.0 };

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

                var result = await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
                string review = result.ToString().Trim();

                if (review.Contains("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine($"  [REVIEWER] Task {task.Id} approved.");  
                }
                else
                {
                    // Step: this needs the '$' prefix, otherwise the placeholders print literally instead of interpolating
                    Console.WriteLine($"  [REVIEWER] Task {task.Id} rejected. Reason: {review}");
                    // Mutate state to force re-work
                    task.IsCompleted = false;
                    task.Description = $"[REVIEWER FEEDBACK] {review}";
                }
            }
        }
    }
}
