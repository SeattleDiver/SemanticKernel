using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

// The Plugins.Web package (BingConnector / WebSearchEnginePlugin) is still marked
// experimental by the Semantic Kernel team — this suppresses that diagnostic.
#pragma warning disable SKEXP0050

namespace Day5Researcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 1: Initialize the Kernel with an OpenAI chat model
            var builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                            ?? throw new Exception("OPENAI_API_KEY environment variable not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Step 2: Register a portable web-search plugin backed by Bing.
            // Unlike a provider-proprietary grounding feature (e.g. Gemini's built-in Google
            // Search tool), this is a normal Semantic Kernel plugin: it works the same way
            // regardless of which chat model is behind IChatCompletionService.
            string bingApiKey = Environment.GetEnvironmentVariable("BING_API_KEY")
                            ?? throw new Exception("BING_API_KEY environment variable not set.");

            var bingConnector = new BingConnector(bingApiKey);
            var webSearchPlugin = new WebSearchEnginePlugin(bingConnector);
            builder.Plugins.AddFromObject(webSearchPlugin, "SearchPlugin");

            Kernel kernel = builder.Build();

            // Step 3: Ask a question that requires real-time knowledge
            string prompt = "What are the top 3 news headlines regarding space exploration today? Summarize each in one sentence.";

            // Step 4: Let the model decide, on its own, whether it needs to call the search
            // plugin to answer the question — the same FunctionChoiceBehavior.Auto() idiom
            // used by every other tool-calling episode in this series.
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var arguments = new KernelArguments(executionSettings);

            Console.WriteLine($"Question: {prompt}\n");
            Console.WriteLine("Agent is thinking and searching the web via the Bing plugin...\n");

            // Step 5: Execute the prompt
            var result = await kernel.InvokePromptAsync(prompt, arguments);

            // Step 6: Display the final result
            Console.WriteLine("--- AI RESEARCH REPORT ---");
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("--------------------------");
        }
    }
}
