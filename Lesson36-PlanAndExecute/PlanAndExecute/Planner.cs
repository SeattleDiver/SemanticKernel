using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace PlanAndExecute
{
    /// <summary>
    /// Generates an ordered execution plan for a goal, optionally regenerating it from scratch
    /// in response to a specific execution failure.
    /// </summary>
    internal class Planner
    {
        private readonly IChatCompletionService _chatService;

        /// <summary>
        /// Creates a planner backed by the given chat completion service.
        /// </summary>
        public Planner(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Produces a plan for the given goal. When <paramref name="failureContext"/> is
        /// supplied, the plan is regenerated from scratch to avoid the described failure,
        /// rather than just retrying the step that failed.
        /// </summary>
        public async Task<ExecutionPlan> GeneratePlanAsync(string goal, string? failureContext = null)
        {
            string failureBlock = failureContext is null
                ? string.Empty
                : $$"""

                    A PREVIOUS ATTEMPT AT THIS PLAN FAILED:
                    {{failureContext}}

                    Regenerate the full plan from scratch, incorporating whatever step is needed
                    to avoid this exact failure. Do not just repeat the failed step unchanged.
                    """;

            string prompt = $$"""
                You are a technical planner. Break the goal below into an ordered list of concise,
                concrete steps (5-8 words each). Output only the steps needed to accomplish the goal.
                {{failureBlock}}

                GOAL:
                {{goal}}

                Output ONLY valid JSON matching this schema:
                {
                    "steps": ["step 1", "step 2", "..."]
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3,
                ResponseFormat = typeof(ExecutionPlan)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<ExecutionPlan>(response.Content ?? "{}") ?? Empty();
            }
            catch (JsonException)
            {
                return Empty();
            }
        }

        /// <summary>
        /// Fallback plan used when the planner's own JSON response can't be parsed.
        /// </summary>
        private static ExecutionPlan Empty() => new()
        {
            Steps = new List<string> { "Failed to parse plan; no steps generated." }
        };
    }
}
