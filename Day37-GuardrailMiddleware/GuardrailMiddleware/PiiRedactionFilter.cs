using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    /// <summary>
    /// Input guardrail: redacts email addresses and phone numbers from the rendered prompt
    /// before it ever reaches the model, rather than just logging what was sent.
    /// </summary>
    internal class PiiRedactionFilter : IPromptRenderFilter
    {
        private static readonly Regex EmailPattern = new(
            @"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);

        private static readonly Regex PhonePattern = new(
            @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);

        /// <summary>
        /// Renders the prompt, then rewrites it in place to strip any detected PII.
        /// </summary>
        public async Task OnPromptRenderAsync(PromptRenderContext context, Func<PromptRenderContext, Task> next)
        {
            await next(context);

            string original = context.RenderedPrompt ?? string.Empty;
            string redacted = EmailPattern.Replace(original, "[REDACTED_EMAIL]");
            redacted = PhonePattern.Replace(redacted, "[REDACTED_PHONE]");

            if (redacted != original)
            {
                Console.WriteLine("[GUARDRAIL] PII redacted from the outgoing prompt before it reached the model.\n");
            }

            context.RenderedPrompt = redacted;
        }
    }
}
