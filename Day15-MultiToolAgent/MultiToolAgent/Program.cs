// Day 15: Multi-Tool Agent
// ---------------------------------------------------------------------------
// Registers two independent plugins (Time, Weather) and lets the model
// decide, on its own, which one (or both) a single request actually needs.
// The sample question requires both tools in the same turn - this is the
// clearest demonstration in the series of real tool *selection*, not just
// tool *execution*.
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace MultiToolAgent
{
    // 1. Define multiple plugins
    public class TimePlugin
    {
        [KernelFunction("GetLocalTime")]
        [Description("Gets the current local time.")]
        public string GetTime()
        {
            Console.WriteLine("[EXECUTING TOOL] GetLocalTime.");
            return DateTime.Now.ToString("F");
        }
    }

    public class WeatherPlugin
    {
        [KernelFunction("GetWeather")]
        [Description("Gets the current weather for a specific city.")]
        public string GetWeather([Description("The city name")] string city)
        {
            Console.WriteLine($"[EXECUTING TOOL] GetWeather: City {city}.");
            return city.Contains("London") ? "15°C and Rainy" : "22°C and Sunny";
        }
    }

    class Program
    {
        static async Task Main(string[] strings)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY was not found");

            // 2. Setup Gemini
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            // 3. Register the plugins
            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<WeatherPlugin>("Weather");

            Kernel kernel = builder.Build();

            // 4. Set the ToolCallBehavior to AutoInvokeKernelFunctions
            // This is what makes it "Agentic" - the Kernel handles the tool-loop
            var settings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };

            string userRequest = "What time is it, and should I bring an umbrella in Salt Lake City today?";
            Console.WriteLine($"User Request: {userRequest}");
            Console.WriteLine("--- AGENT REASONING AND TOOL USER ---");

            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));
            Console.WriteLine($"\nFinal response: {result}");

        }
    }
}