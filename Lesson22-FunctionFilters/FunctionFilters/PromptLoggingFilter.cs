// PromptLoggingFilter
// ---------------------------------------------------------------------------
// A cross-cutting observability filter, entirely provider-agnostic: it runs
// around every prompt render regardless of which connector is registered.
using Microsoft.SemanticKernel;

namespace FunctionFilters
{
    /// <summary>
    /// Intercepts the prompt after variables are injected, but before it reaches the model.
    /// </summary>
    internal class PromptLoggingFilter : IPromptRenderFilter
    {
        /// <summary>Lets the Kernel render the prompt, then logs the fully-rendered text before it's sent to the model.</summary>
        /// <param name="context">The render context, whose <see cref="PromptRenderContext.RenderedPrompt"/> is populated once <paramref name="next"/> runs.</param>
        /// <param name="next">Delegate that performs the actual prompt rendering.</param>
        public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
        {
            // The 'next' delegate allows the Kernel to actually render the prompt
            await next(context);

            // Now that it's rendered, we can inspect or modify it.
            // Step: Use WriteLine (not Write) for the header so the prompt
            // text always starts on its own line - this is a logging filter,
            // so the printed output needs to stay readable.
            Console.WriteLine("\n[PROMPT LOGGER] Intercepted payload headed to the model:");
            Console.WriteLine(context.RenderedPrompt);
        }
    }
}
