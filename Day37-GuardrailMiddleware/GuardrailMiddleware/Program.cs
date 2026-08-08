// Day 37: Safety Guardrail Middleware
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace GuardrailMiddleware
{
    /// <summary>
    /// Entry point demonstrating an input PII-redaction guardrail and an output
    /// content-policy guardrail, both wired in as Semantic Kernel filters.
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Runs two demos: one showing PII stripped from an outgoing prompt, one showing a
        /// policy-violating response replaced before it reaches the console.
        /// </summary>
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
