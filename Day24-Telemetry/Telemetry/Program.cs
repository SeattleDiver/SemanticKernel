// Day 24: Telemetry & Observability
// ---------------------------------------------------------------------------
// The series capstone: wires OpenTelemetry into Semantic Kernel's own
// diagnostic ActivitySource so every prompt render, model call, and plugin
// execution shows up as a traced span with real durations.
// SlowWeatherPlugin's artificial delay makes the tool-execution span clearly
// distinct from the surrounding LLM-call spans in the console output. This
// is provider-agnostic by construction - OTel listens to SK's own
// instrumentation, not to any specific provider, so the tracing keeps
// working identically no matter which connector is registered below.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenTelemetry;
using OpenTelemetry.Trace;

namespace Telemetry
{
    /// <summary>Entry point that wires OpenTelemetry into Semantic Kernel's diagnostic instrumentation and traces one request.</summary>
    class Program
    {
        /// <summary>Configures an OTel console tracer, then runs a tool-calling prompt so its spans (LLM/plugin/LLM) are traced.</summary>
        /// <param name="args">Unused command-line arguments.</param>
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

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);

            // Register our mock plugin
            builder.Plugins.AddFromType<SlowWeatherPlugin>();

            Kernel kernel = builder.Build();

            Console.WriteLine("Starting observability session...\n");

            // 3. Execute a request that requires tool usage
            // By enabling AutoInvoke, Semantic Kernel will automatically handle the tool loop.
            // This generates multiple telemetry spans: one for the initial LLM call,
            // one for the plugin execution, and one for the final LLM summary.
            OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings();
            settings.ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions;

            KernelArguments arguments = new KernelArguments(settings);
            string prompt = "What is the weather like in Seattle right now?";

            Console.WriteLine($"User: {prompt}\n");
            Console.WriteLine("--- EXECUTING & TRACING ---");

            try
            {
                // The tracer is running in the background and will intercept activities here
                var result = await kernel.InvokePromptAsync(prompt, arguments);

                Console.WriteLine($"\nFinal AI Output: {result}");
            }
            catch (Exception ex)
            {
                // A dropped connection or rejected API call shouldn't crash the
                // session with an unhandled exception - and for this lesson it's
                // doubly important, since the trace of *how* it failed is exactly
                // the kind of thing OTel is here to capture.
                Console.WriteLine($"\n[ERROR] The request failed: {ex.Message}");
            }
            finally
            {
                // Ensure all traces are flushed to the console before the program exits,
                // whether the call above succeeded or failed.
                tracerProvider.ForceFlush();
            }

            Console.WriteLine("\nSession complete.  Review Open Telemetry traces above.");
        }
    }
}