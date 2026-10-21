// AuditFunctionFilter
// ---------------------------------------------------------------------------
// Wraps every kernel function invocation with timing and audit logging. That
// includes each plugin (tool) call the model requests, and also the prompt
// itself: InvokePromptAsync compiles the prompt into a KernelFunction (no
// plugin name, an "InvokePromptAsync_<id>" function name), so this filter
// forms an outer audit pair around the whole prompt call. The `next`
// delegate is the actual invocation - this filter could also cancel it
// (by not calling next() and setting context.Result manually) to enforce a
// security policy before a tool ever runs.
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace FunctionFilters
{
    /// <summary>
    /// Intercepts every kernel function invocation (the prompt function and each tool call the AI makes), acting as a security and observability layer.
    /// </summary>
    internal class AuditFunctionFilter : IFunctionInvocationFilter
    {
        /// <summary>Times and logs a function invocation, identified by plugin and function name, before and after letting it run.</summary>
        /// <param name="context">The invocation context describing which function was requested and, after <paramref name="next"/> runs, its result.</param>
        /// <param name="next">Delegate that invokes the actual function (a plugin method or the prompt call); not calling it would veto the operation.</param>
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            // 1. Pre-Execution logic
            // A prompt function created by InvokePromptAsync has no plugin name,
            // so fall back to the function name alone in that case.
            string functionId = string.IsNullOrEmpty(context.Function.PluginName)
                ? context.Function.Name
                : $"{context.Function.PluginName}.{context.Function.Name}";

            var sw = Stopwatch.StartNew();
            Console.WriteLine($"\n[AUDIT] Function requested: {functionId}");

            // Note: You could cancel the execution here by throwing an exception
            // or setting context.Result manually without calling next()

            // 2. Execute the actual function (the C# tool method, or the prompt call)
            await next(context);

            // 3. Post-Execution logic
            sw.Stop();
            Console.WriteLine($"[AUDIT] Finished {functionId}, Duration: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"[AUDIT] Function returned: {context.Result}");
        }
    }
}
