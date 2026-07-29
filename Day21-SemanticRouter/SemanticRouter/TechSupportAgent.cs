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
    /// A domain-speicifc agent that only knows how to handle technical issues.
    /// </summary>
    internal class TechSupportAgent
    {
        private readonly Kernel _isolatedKernel;

        public TechSupportAgent(Kernel baseKernel)
        {
            // CRITICAL: Clone creates a new kernel with the same AI services,
            // but an empty plugin collection, ensuring perfect tool isolation.
            _isolatedKernel = baseKernel.Clone();
            _isolatedKernel.Plugins.AddFromObject(this, "TechPlugin");
        }

        [KernelFunction("ResetPassword")]
        [Description("Resets the password for a specific username.")]
        public string ResetPassword([Description("The user's account name")]string username)
        {
            return $"[SYSTEM API] A password reset link has been emailed to {username}";
        }
        
        public async Task<string> HandleAsync(string input)
        {
#if CHATGPT
            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
#elif GOOGLE
            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };
#endif
            string prompt = $"You are a Tech Support agent.  Solve the user's problem using your tools.  User: {input}";

            var result = await _isolatedKernel.InvokePromptAsync(prompt, new KernelArguments(settings));
            return result.ToString();
        }
    }
}
