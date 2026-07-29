// Day 16: Manual ReAct Loop
// ---------------------------------------------------------------------------
// A hand-rolled ReAct (Thought -> Action -> Observation) loop. Instead of
// letting Semantic Kernel auto-invoke tools, EnableKernelFunctions lets the
// model REQUEST a tool call without executing it - the code below manually
// inspects that request, runs the tool itself, and feeds the result back as
// an "observation" for the next turn. Seeing this by hand demystifies what
// auto-invocation (used everywhere else in the series) does under the hood.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
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
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("Missing key");
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

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
                var settings = new GeminiPromptExecutionSettings { ToolCallBehavior = GeminiToolCallBehavior.EnableKernelFunctions };
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
        }
    }
}