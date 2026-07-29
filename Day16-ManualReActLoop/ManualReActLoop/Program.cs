using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ManualReActLoop
{
    public class ResearchPlugin
    {
        private int _stepCount = 0;

        [KernelFunction("SearchDatabase")]
        [Description("Searches the internal knowledge base for technical specs.")]
        public string Search(string query)
        {
            _stepCount++;
            return query.Contains("Battery", StringComparison.OrdinalIgnoreCase) ? "The Quantum-X battery lasts 24 hours." : "Data not found.";
        }

        [KernelFunction("GetStepCount")]
        [Description("Returns how many research steps have been taken.")]
        public string GetCount() => $"Total steps taken: {_stepCount}";
    }

    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in ManualReActLoop.csproj) before building.");
#else
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Missing key");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";
            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("Missing key");
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
#endif

            // 1. Add stateful object
            var myResearchTool = new ResearchPlugin();
            builder.Plugins.AddFromObject(myResearchTool, "Researcher");
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var history = new ChatHistory(@"
                You are a ReAct Agent. You MUST follow this format for every turn:
                THOUGHT: [Your reasoning]
                ACTION: [The function name to call]
                Wait for the user to provide the OBSERVATION.
                Once you have the answer, provide FINAL ANSWER.");

            string goal = "Find the battery life of the Quantum-X and tell me the total steps taken.";
            history.AddUserMessage(goal);

            Console.WriteLine($"🚀 Goal: {goal}\n");

            // 2. Manual ReAct Loop (Max 5 iterations to prevent infinite loops)
            for (int i = 0; i < 5; i++)
            {
                // Request next step from AI (Enable Call but DON'T Auto-Invoke)
#if CHATGPT
                var settings = new OpenAIPromptExecutionSettings { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(autoInvoke: false) };
#elif GOOGLE
                var settings = new GeminiPromptExecutionSettings { ToolCallBehavior = GeminiToolCallBehavior.EnableKernelFunctions };
#endif
                var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);

                if (string.IsNullOrEmpty(result.Content)) continue;

                // Print the AI's THOUGHT and ACTION
                Console.WriteLine(result.Content);
                history.AddAssistantMessage(result.Content);

                // 3. Check if the AI wants to call a tool
                var toolCalls = result.Items.OfType<FunctionCallContent>().ToList();
                if (toolCalls.Count == 0) break; // No more tools? We are done.

                foreach (var call in toolCalls)
                {
                    // Manually execute the tool to get the OBSERVATION
                    var toolResult = await call.InvokeAsync(kernel);
                    string observation = toolResult?.ToString() ?? "No result";

                    Console.WriteLine($"\nOBSERVATION: {observation}\n");

                    // Add the observation back so the AI can see it in the next turn
                    history.Add(new ChatMessageContent(AuthorRole.Tool, observation) { Items = { new FunctionResultContent(call, toolResult) } });
                }
            }
#endif
        }
    }
}