using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif
using System.ComponentModel;

namespace SemanticRouter
{
    /// <summary>
    /// A domain-specific agent that only knows how to handle financial issues.
    /// </summary>
    internal class BillingAgent
    {
        private readonly Kernel _isolatedKernel;

        public BillingAgent(Kernel baseKernel)
        {
            _isolatedKernel = baseKernel.Clone();
            _isolatedKernel.Plugins.AddFromObject(this, "BillingPlugin");
        }

        [KernelFunction("GetBalance")]
        [Description("Retrieves the current balance for an account.")]
        public string GetBalance([Description("No account ID")] string accountId)
        {
            return $"[SYSTEM API] Account {accountId} has an outstanding balance of $145.50.";
        }

        public async Task<string> HandleAsync(string input)
        {
#if CHATGPT
            var settings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
#elif GOOGLE
            var settings = new GeminiPromptExecutionSettings { ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions };
#endif
            string prompt = $"You are a billing agent.  Provide the user with their financial data.  User: {input}";

            var result = await _isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            return result.ToString();
        }
    }
}
