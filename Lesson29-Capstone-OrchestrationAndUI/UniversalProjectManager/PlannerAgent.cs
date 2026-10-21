using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;

namespace UniversalProjectManager
{
    /// <summary>Breaks the refined goal into concrete tasks, assigning each to a specialist role via the ProjectManagementPlugin.</summary>
    internal class PlannerAgent : IProjectAgent
    {
        private readonly Kernel _baseKernel;

        /// <inheritdoc/>
        public string Name => "Planner";

        /// <summary>Creates a planner that clones the given base kernel and attaches its own isolated tool.</summary>
        /// <param name="baseKernel">The shared kernel to clone AI service registrations from.</param>
        public PlannerAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;
        }

        /// <summary>Breaks <see cref="ProjectState.RefinedGoal"/> into tasks via the model's autonomous CreateTask tool calls.</summary>
        /// <param name="state">The shared project state to read the goal from and write tasks to.</param>
        public async Task ExecuteAsync(ProjectState state)
        {
            // Clone to isolate tools specifically for the Planner
            Kernel isolatedKernel = _baseKernel.Clone();

            // Attach the state-aware plugin
            isolatedKernel.Plugins.AddFromObject(new ProjectManagementPlugin(state), "ProjectManager");

            string prompt = $@"
                You are a Technical Project Manager.
                Your objective is: {state.RefinedGoal}

                Break this objective into exactly 2 techincal implementation tasks.
                Assign both tasks to the 'Developer' role using the CreateTask tool.
                Do not write code.  Just plan.
                ";

            // Enable AutoInvoke so the LLM can call CreateTAsk autonomously
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.2 // Low temperatore for deterministic planning
            };

            try
            {
                await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            }
            catch (Exception ex)
            {
                // A network hiccup, rate limit, or content filter here would otherwise crash
                // the whole console session. Report it and leave state.Tasks empty instead;
                // the Developer stage already tolerates finding zero pending tasks.
                Console.WriteLine($"[{Name}] Failed to plan tasks: {ex.Message}");
            }
        }
    }
}
