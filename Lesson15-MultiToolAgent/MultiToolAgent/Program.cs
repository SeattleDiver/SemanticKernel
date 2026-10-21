// Day 15: Multi-Tool Agent
// ---------------------------------------------------------------------------
// Registers two independent plugins (Time, Weather) and lets the model
// decide, on its own, which one (or both) a single request actually needs.
// The sample question requires both tools in the same turn - this is the
// clearest demonstration in the series of real tool *selection*, not just
// tool *execution*.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MultiToolAgent
{
    // 1. The plugins are defined in their own files: TimePlugin.cs and WeatherPlugin.cs.
    /// <summary>Entry point that registers two independent plugins and lets the model decide which one(s) a request needs.</summary>
    class Program
    {
        /// <summary>Asks a compound question requiring both plugins in one turn and prints the agent's synthesized answer.</summary>
        /// <param name="strings">Unused command-line arguments.</param>
        static async Task Main(string[] strings)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY was not found");

            // 2. Setup OpenAI
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);

            // 3. Register the plugins
            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<WeatherPlugin>("Weather");

            Kernel kernel = builder.Build();

            // 4. Set the ToolCallBehavior to AutoInvokeKernelFunctions
            // This is what makes it "Agentic" - the Kernel handles the tool-loop
            var settings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
            };

            string userRequest = "What time is it, and should I bring an umbrella in Salt Lake City today?";
            Console.WriteLine($"User Request: {userRequest}");
            Console.WriteLine("--- AGENT REASONING AND TOOL USER ---");

            // 5. Guard the model call - OpenAI's API call and the auto-invoked
            // tool round trips behind it are the riskiest part of this program;
            // without a catch here, a network hiccup or API error would crash
            // the whole console app instead of just failing this one request.
            try
            {
                var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));
                Console.WriteLine($"\nFinal response: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nAgent run failed: {ex.Message}");
            }
        }
    }
}