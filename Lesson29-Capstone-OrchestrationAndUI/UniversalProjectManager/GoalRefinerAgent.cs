using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace UniversalProjectManager
{
    /// <summary>
    /// A preliminary agent responsible for taking a raw user input and rewriting it
    /// into a clear, one-sentence technical goal.
    /// </summary>
    internal class GoalRefinerAgent : IProjectAgent
    {
        private readonly Kernel _kernel;

        /// <inheritdoc/>
        public string Name => "GoalRefiner";

        /// <summary>Creates a goal refiner that uses the given kernel to normalize raw requests.</summary>
        /// <param name="kernel">The kernel used to invoke the refinement prompt.</param>
        public GoalRefinerAgent(Kernel kernel)
        {
            _kernel = kernel;
        }

        /// <summary>Rewrites <see cref="ProjectState.OriginalRequest"/> into a one-sentence technical goal and stores it back on the state.</summary>
        /// <param name="state">The shared project state to read the raw request from and write the refined goal to.</param>
        public async Task ExecuteAsync(ProjectState state)
        {
            // Use the kernel to refine the goal based on the original request
            var prompt = $@"
                You are a highly analytical Project Manager.
                Take the user's raw request and refine it into a clear, 1-sentence technical goal.
                Strip away conversational filler.

                RAW REQUEST: {state.OriginalRequest}
                ".Trim();

            // Execute the prompt with a low temperature to ensure deterministic and professional output
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.1
            };

            try
            {
                var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(settings));

                // Mutate the shared state with the refined goal
                state.RefinedGoal = result.ToString().Trim();
            }
            catch (Exception ex)
            {
                // A network hiccup, rate limit, or content filter here would otherwise crash
                // the whole console session. Report it and leave RefinedGoal at its default instead.
                Console.WriteLine($"[{Name}] Failed to refine goal: {ex.Message}");
            }
        }
    }
}
