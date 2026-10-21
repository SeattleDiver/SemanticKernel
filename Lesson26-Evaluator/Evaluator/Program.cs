using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Evaluator
{
    /// <summary>A single test case: a question to ask the target agent and the known-correct answer to judge it against.</summary>
    /// <param name="Question">The question to ask the target agent.</param>
    /// <param name="GroundTruth">The known-correct answer the judge compares the response against.</param>
    public record TestCase(string Question, string GroundTruth);

    /// <summary>Entry point that runs a fixed test suite through a target agent and scores each answer with an LLM judge.</summary>
    internal class Program
    {
        /// <summary>Runs each test case through the target agent, judges the response, and prints a pass/fail verdict.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Setup the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new ArgumentNullException("OPENAI_API_KEY is missing");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Initialize our clean, single responsible agents
            var targetAgent = new TargetAgent(chatService);
            var judgeAgent = new JudgeAgent(chatService);

            // Define our automated test cases
            var tests = new[]
            {
                new TestCase(Question: "What is the capital of France?",
                            GroundTruth: "The capital of France is Paris"),

                new TestCase(Question: "How long is the return window for a Premium laptop?",
                            GroundTruth: "Premium laptops have a strict 14-day return window.")
            };

            Console.WriteLine("AI Evaluation Suite Started...");

            // Run the Suite
            for (int i = 0; i < tests.Length; i++)
            {
                Console.WriteLine($"--- Running Test {i + 1} ---");
                Console.WriteLine($"Q: {tests[i].Question}");

                // Target Agent attempts to answer
                string agentResponse = await targetAgent.AskQuestionAsync(tests[i].Question);
                Console.WriteLine($"A: {agentResponse}");

                // Judge evaluates the answer against the Ground Truth
                EvaluationResult evaluation = await judgeAgent.EvaluateAsync(
                    tests[i].Question,
                    agentResponse,
                    tests[i].GroundTruth);

                string passFail = evaluation.Passed ? "PASS" : "FAIL";
                Console.WriteLine($"\nVerdict: {passFail} (Score: {evaluation.Score}/5)");
                Console.WriteLine($"Judge's Reasoning: {evaluation.Reasoning}\n");
            }
        }
    }
}
