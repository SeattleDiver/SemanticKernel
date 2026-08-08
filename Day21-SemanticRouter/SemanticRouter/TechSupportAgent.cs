// TechSupportAgent
// ---------------------------------------------------------------------------
// A domain-isolated specialist: it clones the base kernel (empty plugin
// collection) and registers only its own ResetPassword tool, so it has no
// way to reach BillingAgent's tools even under an adversarial prompt.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

namespace SemanticRouter
{
    /// <summary>
    /// A domain-specific agent that only knows how to handle technical issues.
    /// </summary>
    internal class TechSupportAgent
    {
        private readonly Kernel _isolatedKernel;

        /// <summary>Clones the base kernel into an isolated instance and registers only this agent's own tool.</summary>
        /// <param name="baseKernel">The shared kernel to clone AI service registrations from.</param>
        public TechSupportAgent(Kernel baseKernel)
        {
            // CRITICAL: Clone creates a new kernel with the same AI services,
            // but an empty plugin collection, ensuring perfect tool isolation.
            _isolatedKernel = baseKernel.Clone();
            _isolatedKernel.Plugins.AddFromObject(this, "TechPlugin");
        }

        /// <summary>Resets the password for a specific username.</summary>
        /// <param name="username">The user's account name.</param>
        /// <returns>A confirmation message describing the simulated password reset.</returns>
        [KernelFunction("ResetPassword")]
        [Description("Resets the password for a specific username.")]
        public string ResetPassword([Description("The user's account name")]string username)
        {
            return $"[SYSTEM API] A password reset link has been emailed to {username}";
        }

        /// <summary>Solves the user's technical issue using this agent's isolated kernel and tool.</summary>
        /// <param name="input">The user's request.</param>
        /// <returns>The agent's response.</returns>
        public async Task<string> HandleAsync(string input)
        {
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };
            string prompt = $"You are a Tech Support agent.  Solve the user's problem using your tools.  User: {input}";

            var result = await _isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            return result.ToString();
        }
    }
}
