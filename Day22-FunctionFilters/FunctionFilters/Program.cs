using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace FunctionFilters
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in FunctionFilters.csproj) before building.");
#else
            IKernelBuilder builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-3.1-pro", apiKey);
#endif

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
#if CHATGPT
            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
#elif GOOGLE
            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };
#endif

            // This single line triggers the prompt filter, the model API,
            // the function filter, the C# tool, and the final model summary!
            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));

            Console.WriteLine($"\n[AI FINAL RESPONSE]: {result}");
#endif
        }
    }
}
