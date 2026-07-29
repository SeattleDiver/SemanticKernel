using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Web;
using Microsoft.SemanticKernel.Plugins.Web.Bing;

// The Plugins.Web package (BingConnector / WebSearchEnginePlugin) is still marked
// experimental by the Semantic Kernel team — this suppresses that diagnostic.
#pragma warning disable SKEXP0050
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace Day5Researcher
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in Researcher.csproj) before building.");
#else
            // Step 1: Initialize the Kernel
            var builder = Kernel.CreateBuilder();

#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                            ?? throw new Exception("OPENAI_API_KEY environment variable not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Step 2: Register a portable web-search plugin backed by Bing.
            // Unlike a provider-proprietary grounding feature (e.g. Gemini's built-in Google
            // Search tool below), this is a normal Semantic Kernel plugin: it works the same
            // way regardless of which chat model is behind IChatCompletionService.
            string bingApiKey = Environment.GetEnvironmentVariable("BING_API_KEY")
                            ?? throw new Exception("BING_API_KEY environment variable not set.");

            var bingConnector = new BingConnector(bingApiKey);
            var webSearchPlugin = new WebSearchEnginePlugin(bingConnector);
            builder.Plugins.AddFromObject(webSearchPlugin, "SearchPlugin");
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? throw new Exception("GEMINI_API_KEY environment variable not set.");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

            // Notice: We do NOT add any Custom Search Plugins to the builder - Gemini's
            // grounding is a built-in, provider-proprietary feature configured below via
            // ExtensionData, not a Semantic Kernel plugin.
#endif

            Kernel kernel = builder.Build();

            // Step 3: Ask a question that requires real-time knowledge
            string prompt = "What are the top 3 news headlines regarding space exploration today? Summarize each in one sentence.";

#if CHATGPT
            // Step 4: Let the model decide, on its own, whether it needs to call the search
            // plugin to answer the question — the same FunctionChoiceBehavior.Auto() idiom
            // used by every other tool-calling episode in this series.
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };
#elif GOOGLE
            // Step 4: Enable Gemini's native Google Search grounding.
            // CRITICAL: Do NOT use ToolCallBehavior here. If we do, Semantic Kernel
            // will overwrite the JSON tools array and Gemini will lose internet access.
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
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
#endif

            var arguments = new KernelArguments(executionSettings);

            Console.WriteLine($"Question: {prompt}\n");
#if CHATGPT
            Console.WriteLine("Agent is thinking and searching the web via the Bing plugin...\n");
#elif GOOGLE
            Console.WriteLine("Agent is thinking and utilizing Gemini's built-in Google Search grounding...\n");
#endif

            // Step 5: Execute the prompt
            var result = await kernel.InvokePromptAsync(prompt, arguments);

            // Step 6: Display the final result
            Console.WriteLine("--- AI RESEARCH REPORT ---");
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("--------------------------");
#endif
        }
    }
}
