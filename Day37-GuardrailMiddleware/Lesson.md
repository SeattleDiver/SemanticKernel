# Day 37 — Safety Guardrail Middleware

## Overview

This lesson wires in two guardrails as real Semantic Kernel filters: one
that strips PII out of a prompt before it ever reaches the model, and one
that inspects the model's finished response and swaps in a safe refusal if
it violates a business content policy. Both use the exact filter
interfaces Day 22 introduced - the difference is what they do inside them.

## Prerequisites

- **Day 22 (FunctionFilters)** - for contrast. Day 22's `PromptLoggingFilter`
  and `AuditFunctionFilter` only observe: they print what's happening but
  never change it. This lesson's filters actively rewrite the prompt and
  the result.
- Comfort with `IPromptRenderFilter` and `IFunctionInvocationFilter`, and
  registering filters via `builder.Services.AddSingleton<...>()`.

## Setup

- .NET 10 SDK
- A Gemini API key, available via the `GEMINI_API_KEY` environment variable
- NuGet packages:
  - `Microsoft.SemanticKernel` `1.78.0`
  - `Microsoft.SemanticKernel.Connectors.Google` `1.79.0-alpha`

```
dotnet add package Microsoft.SemanticKernel --version 1.78.0
dotnet add package Microsoft.SemanticKernel.Connectors.Google --version 1.79.0-alpha
```

## Core Concepts

**Logging observes; a guardrail intervenes.** The distinction Day 22 left
open is exactly the gap this lesson closes: a filter that only prints
`context.RenderedPrompt` is an audit trail. A filter that *reassigns*
`context.RenderedPrompt` before returning changes what the model actually
sees. Same interface, fundamentally different role.

**Input guardrails and output guardrails protect against different things.**
Redacting PII on the way in protects the *user* - their personal data never
leaves the process in the first place, regardless of what the model would
have done with it. Rejecting policy-violating content on the way out
protects the *business* - a rule like "never name a competitor" has nothing
to do with safety in the harmful-content sense; it's a plain business
constraint the model was never specifically trained to follow, so nothing
stops it from violating it unless something checks afterward.

**A semantic prompt is still a function invocation.** `IFunctionInvocationFilter`
sounds like it's only for plugin/tool calls (that's all Day 22 showed it
doing), but `kernel.InvokePromptAsync(...)` compiles the prompt into a
`KernelFunction` internally and invokes it the same way - so this filter
wraps the *whole* prompt call, not just tool calls, which is what makes
inspecting the final response text here possible at all.

**Deterministic checks, not model judgment.** Both guardrails use plain
regex/string matching, not a second LLM call asking "is this OK?" A second
model call would be slower, cost tokens, and could itself be wrong or
inconsistent between runs. For rules that are actually checkable in code -
"does this look like an email," "does this contain this literal name" -
code is strictly better than another model in the loop.

## Full Walkthrough / Code

### `PiiRedactionFilter.cs`

```csharp
using System.Text.RegularExpressions;
using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    internal class PiiRedactionFilter : IPromptRenderFilter
    {
        private static readonly Regex EmailPattern = new(
            @"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);

        private static readonly Regex PhonePattern = new(
            @"\b\d{3}[-.]?\d{3}[-.]?\d{4}\b", RegexOptions.Compiled);

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
```

### `ContentPolicyFilter.cs`

```csharp
using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    internal class ContentPolicyFilter : IFunctionInvocationFilter
    {
        private static readonly string[] BannedTerms = { "CloudPeak", "Nimbus" };

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
```

### `Program.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            builder.Services.AddSingleton<IPromptRenderFilter, PiiRedactionFilter>();
            builder.Services.AddSingleton<IFunctionInvocationFilter, ContentPolicyFilter>();

            Kernel kernel = builder.Build();

            Console.WriteLine("=== Demo 1: Input PII Redaction ===");
            string userMessage1 =
                "Hi, my email is jane.doe@example.com and my phone is 555-123-4567. " +
                "In one sentence, apologize for my order being late.";
            Console.WriteLine($"User: {userMessage1}\n");

            var reply1 = await kernel.InvokePromptAsync(userMessage1);
            Console.WriteLine($"Assistant: {reply1}\n");

            Console.WriteLine("=== Demo 2: Output Content-Policy Rejection ===");
            string userMessage2 =
                "Write a two-sentence comparison of our product against CloudPeak, and be " +
                "sure to mention CloudPeak by name.";
            Console.WriteLine($"User: {userMessage2}\n");

            var reply2 = await kernel.InvokePromptAsync(userMessage2);
            Console.WriteLine($"Assistant: {reply2}");
        }
    }
}
```

## Explanation

`ContentPolicyFilter` builds its replacement with
`new FunctionResult(context.Result!, "...")` - an overload that takes the
*existing* `FunctionResult` and produces a new one carrying a different
value, rather than constructing one from scratch. That matters because a
`FunctionResult` carries more than just the text (which function produced
it, culture info, etc.); reusing the original preserves all of that and
only swaps the value the rest of the pipeline will see.

Demo 2's prompt explicitly asks the model to name "CloudPeak" - that's
deliberate, the same way Day 34's task was deliberately over-specified. A
guardrail lesson that only sometimes triggers its own guardrail doesn't
teach anything reliably on camera; forcing the violation to happen every
run is what makes the rejection path itself the thing being demonstrated,
not a matter of luck.

Both filters are read-only about *whether* to act - `PiiRedactionFilter`
always tries its regexes and only logs when something actually changed;
`ContentPolicyFilter` always checks and only replaces when a banned term is
actually found. Neither filter assumes it will find something every time,
which is exactly how they'd behave correctly on real, unscripted input too.

## Expected Result

A real run produced this exact transcript:

```
=== Demo 1: Input PII Redaction ===
User: Hi, my email is jane.doe@example.com and my phone is 555-123-4567. In one sentence, apologize for my order being late.

[GUARDRAIL] PII redacted from the outgoing prompt before it reached the model.

Assistant: We sincerely apologize for the delay in processing your recent order and for any inconvenience this may have caused.

=== Demo 2: Output Content-Policy Rejection ===
User: Write a two-sentence comparison of our product against CloudPeak, and be sure to mention CloudPeak by name.

[GUARDRAIL] Output blocked - mentioned banned term "CloudPeak". Replacing with a safe response.

Assistant: I can't discuss competitor products by name. I'm happy to describe our own product's capabilities instead.
```

Demo 1's assistant reply never references the caller's email or phone
number - not because the model chose to omit them, but because they were
never in the prompt it saw. Demo 2's reply is the exact hardcoded refusal
text, not a model-generated one, confirming the original (policy-violating)
response was fully discarded rather than edited.
