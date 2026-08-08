// Day 36: Closed-Loop Plan-and-Execute
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace PlanAndExecute
{
    /// <summary>
    /// Entry point that plans, executes, and - on failure - genuinely replans a deployment goal.
    /// </summary>
    internal class Program
    {
        private const int MaxAttempts = 3;
        private const string Goal = "Migrate the orders database to the new schema with zero downtime.";

        /// <summary>
        /// Runs the plan/execute loop, feeding any execution failure back into the planner as
        /// the reason for a full replan, until it succeeds or the attempt cap is reached.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var planner = new Planner(chatService);

            Console.WriteLine($"GOAL: {Goal}\n");

            string? failureContext = null;

            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                Console.WriteLine($"=== Attempt {attempt}: Plan ===");
                ExecutionPlan plan = await planner.GeneratePlanAsync(Goal, failureContext);

                for (int i = 0; i < plan.Steps.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {plan.Steps[i]}");
                }

                Console.WriteLine("\n=== Executing ===");
                ExecutionResult result = SimulatedDeploymentEnvironment.RunPlan(plan);

                if (result.Success)
                {
                    Console.WriteLine($"SUCCESS: {result.Message}");
                    return;
                }

                Console.WriteLine($"FAILURE: {result.Message}\n");
                failureContext = result.Message;

                if (attempt == MaxAttempts)
                {
                    Console.WriteLine("Exhausted replanning attempts without a successful execution.");
                    return;
                }
            }
        }
    }
}
