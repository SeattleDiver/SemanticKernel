using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FunctionFilters
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

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
            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            // This single line triggers the prompt filter, the OpenAI API,
            // the function filter, the C# tool, and the final OpenAI summary!
            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));

            Console.WriteLine($"\n[AI FINAL RESPONSE]: {result}");
        }
    }
}
