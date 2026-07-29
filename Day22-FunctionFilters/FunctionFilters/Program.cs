// Day 22: Prompt and Function Filters
// ---------------------------------------------------------------------------
// Semantic Kernel Filters - think ASP.NET Core middleware, but for AI calls.
// IPromptRenderFilter (PromptLoggingFilter.cs) intercepts the fully-rendered
// prompt right before it leaves the process; IFunctionInvocationFilter
// (AuditFunctionFilter.cs) wraps every plugin call with timing/audit logic.
// Both are registered once via DI and then apply to every prompt/tool call
// automatically - SecureDatabasePlugin.cs itself stays free of any
// logging/observability code.
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace FunctionFilters
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            // 1. Register our Custom Filters via Dependency Injection
            builder.Services.AddSingleton<IPromptRenderFilter, PromptLoggingFilter>();
            builder.Services.AddSingleton<IFunctionInvocationFilter, AuditFunctionFilter>();

            // 2. Register the Plugin
            builder.Plugins.AddFromType<SecureDatabasePlugin>("Database");

            Kernel kernel = builder.Build();

            Console.WriteLine("System online with middleware filters active.");

            string userRequest = "Can you look up the current balance for customer ID CUST-778?";
            Console.WriteLine($"\nUser: {userRequest}");

            // 3. Execute with AutoInvoke so the LLM triggers the function filter
            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };

            // This single line triggers the prompt fileter, the Gemini API,
            // the function filter, the C# tool, and the final Gemini summary!
            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));

            Console.WriteLine($"\n[AI FINAL RESPONSE]: {result}");
        }
    }
}
