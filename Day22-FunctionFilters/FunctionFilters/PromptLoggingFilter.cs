// PromptLoggingFilter
// ---------------------------------------------------------------------------
// A cross-cutting observability filter, entirely provider-agnostic: it runs
// around every prompt render regardless of which connector is registered.
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

            // Now that it's rendered, we can inspect or modify it.
            // Step: Use WriteLine (not Write) for the header so the prompt
            // text always starts on its own line - this is a logging filter,
            // so the printed output needs to stay readable.
            Console.WriteLine("\n[PROMPT LOGGER] Intercepted payload headed to Gemini:");
            Console.WriteLine(context.RenderedPrompt);
        }
    }
}
