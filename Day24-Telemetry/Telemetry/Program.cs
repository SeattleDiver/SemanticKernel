using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Telemetry
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Configure OpenTelemetry Tracing
            // We use explicit lines rather than fluent chaining for clarity.
            TracerProviderBuilder traceBuilder = Sdk.CreateTracerProviderBuilder();

            // Tell Open Telemetry to listen to all diagnostic events emitted by Semantic Kernel
            traceBuilder.AddSource("Microsoft.SemanticKernel*");

            // Output the raw trace data directly to our console window for this tutorial
            traceBuilder.AddConsoleExporter();

            // Build and start the tracer process
            using TracerProvider tracerProvider = traceBuilder.Build();

            // 2. Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Register our mock plugin
            builder.Plugins.AddFromType<SlowWeatherPlugin>();

            Kernel kernel = builder.Build();

            Console.WriteLine("Starting observability session...\n");

            // 3. Execute a request that requires tool usage
            // Be enabling AutoInvoke, Semantic Kernel will automatically handle the tool loop.
            // This generates multiple telemetry spans: one for the initial LLM call,
            // one for the plugin execution, and one for the final LLM summary.
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings();
            settings.FunctionChoiceBehavior = FunctionChoiceBehavior.Auto();

            KernelArguments arguments = new KernelArguments(settings);
            string prompt = "What is the weather like in Seattle right now?";

            Console.WriteLine($"User: {prompt}\n");
            Console.WriteLine("--- EXECUTING & TRACING ---");

            // The tracer is running in the background and will intercept activities here
            var result = await kernel.InvokePromptAsync(prompt, arguments);

            Console.WriteLine($"\nFinal AI Output: {result}");

            // Ensure all traces are flushed to the console before the program exits.
            tracerProvider.ForceFlush();
            Console.WriteLine("\nSession complete.  Review Open Telemetry traces above."); 

        }
    }
}