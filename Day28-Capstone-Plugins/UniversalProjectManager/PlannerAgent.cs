using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalProjectManager
{
    internal class PlannerAgent : IProjectAgent
    {
        private readonly Kernel _baseKernel;
        public string Name => "Planner";

        public PlannerAgent(Kernel baseKernel)
        {
            _baseKernel = baseKernel;                
        }

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
            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.2 // Low temperatore for deterministic planning
            };

            try
            {
                await isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            }
            catch (Exception ex)
            {
                // Step: a network hiccup, rate limit, or content filter here would otherwise crash
                // the whole console session. Report it and leave state.Tasks empty instead;
                // the Developer stage already tolerates finding zero pending tasks.
                Console.WriteLine($"[{Name}] Failed to plan tasks: {ex.Message}");
            }
        }
    }
}
