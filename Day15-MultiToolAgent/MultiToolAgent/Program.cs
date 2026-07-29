using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

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
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in MultiToolAgent.csproj) before building.");
#else
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY was not found");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            // 2. Setup OpenAI
            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY was not found");

            // 2. Setup Gemini
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
#endif

            // 3. Register the plugins
            builder.Plugins.AddFromType<TimePlugin>("Time");
            builder.Plugins.AddFromType<WeatherPlugin>("Weather");

            Kernel kernel = builder.Build();

            // 4. Set the tool-calling behavior to Auto
            // This is what makes it "Agentic" - the Kernel handles the tool-loop
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

            string userRequest = "What time is it, and should I bring an umbrella in Salt Lake City today?";
            Console.WriteLine($"User Request: {userRequest}");
            Console.WriteLine("--- AGENT REASONING AND TOOL USER ---");

            var result = await kernel.InvokePromptAsync(userRequest, new KernelArguments(settings));
            Console.WriteLine($"\nFinal response: {result}");
#endif
        }
    }
}