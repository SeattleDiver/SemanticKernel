using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY was not found");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            // 2. Setup OpenAI
            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // 3. Register the plugins
            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<WeatherPlugin>("Weather");

            Kernel kernel = builder.Build();

            // 4. Set the FunctionChoiceBehavior to Auto
            // This is what makes it "Agentic" - the Kernel handles the tool-loop
            var settings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            string userRequest = "What time is it, and should I bring an umbrella in Salt Lake City today?";
            Console.WriteLine($"User Request: {userRequest}");
            Console.WriteLine("--- AGENT REASONING AND TOOL USER ---");

            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));
            Console.WriteLine($"\nFinal response: {result}");

        }
    }
}