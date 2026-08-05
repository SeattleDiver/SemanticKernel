using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace UniversalProjectManager
{
    /// <summary>
    /// A preliminary agent responsible for taking a raw user input and converting it 
    /// into a professional, actionable project specification.
    /// </summary>
    internal class GoalRefinerAgent : IProjectAgent
    {
        private readonly Kernel _kernel;
        public string Name => "GoalRefiner";

        public GoalRefinerAgent(Kernel kernel) 
        {
            _kernel = kernel;
        }

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
            var settings = new GeminiPromptExecutionSettings 
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
                // Step: a network hiccup, rate limit, or content filter here would otherwise crash
                // the whole console session. Report it and leave RefinedGoal at its default instead.
                Console.WriteLine($"[{Name}] Failed to refine goal: {ex.Message}");
            }
        }
    }
}
