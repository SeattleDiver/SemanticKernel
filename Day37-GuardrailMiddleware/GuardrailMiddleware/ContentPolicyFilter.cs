using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    /// <summary>
    /// Output guardrail: inspects the model's response after it's generated and replaces it
    /// with a safe refusal if it violates a business content policy, rather than just logging it.
    /// </summary>
    internal class ContentPolicyFilter : IFunctionInvocationFilter
    {
        private static readonly string[] BannedTerms = { "CloudPeak", "Nimbus" };

        /// <summary>
        /// Lets the invocation run, then checks the result for banned terms and swaps in a
        /// safe response if any are found.
        /// </summary>
        public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            await next(context);

            string text = context.Result?.ToString() ?? string.Empty;
            string? violated = BannedTerms.FirstOrDefault(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

            if (violated is not null)
            {
                Console.WriteLine($"[GUARDRAIL] Output blocked - mentioned banned term \"{violated}\". Replacing with a safe response.\n");
                context.Result = new FunctionResult(
                    context.Result!,
                    "I can't discuss competitor products by name. I'm happy to describe our own product's capabilities instead.");
            }
        }
    }
}
