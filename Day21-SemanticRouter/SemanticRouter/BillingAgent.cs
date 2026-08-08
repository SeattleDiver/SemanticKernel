// BillingAgent
// ---------------------------------------------------------------------------
// The billing counterpart to TechSupportAgent: its own cloned, isolated
// kernel with only the GetBalance tool registered - it physically cannot
// reset a password, no matter how it's asked.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

namespace SemanticRouter
{
    /// <summary>
    /// A domain-specific agent that only knows how to handle financial issues.
    /// </summary>
    internal class BillingAgent
    {
        private readonly Kernel _isolatedKernel;

        /// <summary>Clones the base kernel into an isolated instance and registers only this agent's own tool.</summary>
        /// <param name="baseKernel">The shared kernel to clone AI service registrations from.</param>
        public BillingAgent(Kernel baseKernel)
        {
            _isolatedKernel = baseKernel.Clone();
            _isolatedKernel.Plugins.AddFromObject(this, "BillingPlugin");
        }

        /// <summary>Retrieves the current balance for an account.</summary>
        /// <param name="accountId">The account ID to look up.</param>
        /// <returns>A message describing the simulated account balance.</returns>
        [KernelFunction("GetBalance")]
        [Description("Retrieves the current balance for an account.")]
        public string GetBalance([Description("The account ID")] string accountId)
        {
            return $"[SYSTEM API] Account {accountId} has an outstanding balance of $145.50.";
        }

        /// <summary>Provides the user with their financial data using this agent's isolated kernel and tool.</summary>
        /// <param name="input">The user's request.</param>
        /// <returns>The agent's response.</returns>
        public async Task<string> HandleAsync(string input)
        {
            var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions };
            string prompt = $"You are a billing agent.  Provide the user with their financial data.  User: {input}";

            var result = await _isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            return result.ToString();
        }
    }
}
