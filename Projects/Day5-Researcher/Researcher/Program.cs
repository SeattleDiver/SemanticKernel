using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Day5Researcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 1: Initialize the Kernel with Gemini 2.5 Flash
            var builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? throw new Exception("GEMINI_API_KEY environment variable not set.");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            // Notice: We do NOT add any Custom Search Plugins to the builder!
            Kernel kernel = builder.Build();

            // Step 2: Ask a question that requires real-time knowledge
            string prompt = "What are the top 3 news headlines regarding space exploration today? Summarize each in one sentence.";

            // Step 3: Enable Gemini's Native Google Search Grounding
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
                // CRITICAL: Do NOT use ToolCallBehavior here. If we do, Semantic Kernel 
                // will overwrite the JSON tools array and Gemini will lose internet access.

                ExtensionData = new Dictionary<string, object>
                {
                    {
                        "tools", new[]
                        { 
                            // The Gemini API requires camelCase "googleSearch"
                            new { googleSearch = new { } }
                        }
                    }
                }
            };

            var arguments = new KernelArguments(executionSettings);

            Console.WriteLine($"Question: {prompt}\n");
            Console.WriteLine("Agent is thinking and utilizing Gemini's built-in Google Search grounding...\n");

            // Step 4: Execute the prompt
            var result = await kernel.InvokePromptAsync(prompt, arguments);

            // Step 5: Display the final result
            Console.WriteLine("--- AI RESEARCH REPORT ---");
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("--------------------------");
        }
    }
}