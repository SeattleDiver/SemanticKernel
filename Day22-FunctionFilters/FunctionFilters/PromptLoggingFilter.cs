using Microsoft.SemanticKernel;

namespace FunctionFilters
{
    /// <summary>
    /// Intercepts the prompt after varaiables are injected, but berfore it reaches Gemini
    /// </summary>
    internal class PromptLoggingFilter : IPromptRenderFilter
    {
        public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
        {
            // The 'next' delegate allows the Kernel to actually render the prompt
            await next(context);

            // Now that it's rendered, we can inspect or modify it
            Console.Write("\n[PROMPT LOGGER] Intercepted payload headed to Gemini:");
            Console.WriteLine(context.RenderedPrompt);
        }
    }
}
