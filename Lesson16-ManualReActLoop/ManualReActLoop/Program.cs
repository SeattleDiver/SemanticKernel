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
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ManualReActLoop
{
    /// <summary>Stateful native plugin exposing a mock knowledge-base search and a running count of research steps taken.</summary>
    public class ResearchPlugin
    {
        private int _stepCount = 0;

        /// <summary>Searches the internal knowledge base for technical specs matching the query.</summary>
        /// <param name="query">The search query.</param>
        /// <returns>The matching spec, or "Data not found." if the query doesn't match the mock knowledge base.</returns>
        [KernelFunction("SearchDatabase")]
        [Description("Searches the internal knowledge base for technical specs.")]
        public string Search(string query)
        {
            _stepCount++;
            return query.Contains("Battery", StringComparison.OrdinalIgnoreCase) ? "The Quantum-X battery lasts 24 hours." : "Data not found.";
        }

        /// <summary>Returns how many research steps have been taken so far.</summary>
        /// <returns>A message stating the total number of <see cref="Search"/> calls made.</returns>
        [KernelFunction("GetStepCount")]
        [Description("Returns how many research steps have been taken.")]
        public string GetCount() => $"Total steps taken: {_stepCount}";
    }

    /// <summary>Entry point that hand-rolls a ReAct (Thought/Action/Observation) loop instead of relying on SK auto-invocation.</summary>
    class Program
    {
        /// <summary>Runs up to 5 manual request/execute/observe iterations, invoking model-requested tools by hand each turn.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Missing key");
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);

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
                var settings = new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions };
                var result = await chatService.GetChatMessageContentAsync(history, settings, kernel);

                // 3. Check for a tool-call request BEFORE looking at Content. Models
                // often return an empty Content string when they are requesting a
                // function call instead of talking - checking Content first (and
                // skipping the turn when it's empty) would silently ignore that
                // function call forever and stall the loop until the iteration cap.
                var toolCalls = result.Items.OfType<FunctionCallContent>().ToList();

                // Print the AI's THOUGHT and ACTION text, if any was produced this turn.
                if (!string.IsNullOrEmpty(result.Content)) Console.WriteLine(result.Content);
                history.Add(result); // Keep the full response (text + FunctionCallContent) in history.

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