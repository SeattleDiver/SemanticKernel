// AuditFunctionFilter
// ---------------------------------------------------------------------------
// Wraps every plugin invocation with timing and audit logging. The `next`
// delegate is the actual C# method call - this filter could also cancel it
// (by not calling next() and setting context.Result manually) to enforce a
// security policy before a tool ever runs.
using Microsoft.SemanticKernel;
using System.Diagnostics;

namespace FunctionFilters
{
    /// <summary>
    /// Intercepts tool calls made by the AAI, acting as a security and observability layer.
    /// </summary>
    internal class AuditFunctionFilter : IFunctionInvocationFilter
    {
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            // 1. Pre-Execution logic
            var sw = Stopwatch.StartNew();
            Console.WriteLine($"\n[AUDIT] AI requested tool execution: {context.Function.PluginName}");

            // Note: You could cancel the execution here by throwing an exception
            // or setting context.Result manually without calling next()

            // 2. Execute the actual C# method
            await next(context);

            // 3. Post-Execution logic
            sw.Stop();
            Console.WriteLine($"[AUDIT] Finished {context.Function.Name}, Duration: {sw.ElapsedMilliseconds}ms");
            Console.WriteLine($"[AUDIT] Tool returned: {context.Result}");
        }
    }
}
