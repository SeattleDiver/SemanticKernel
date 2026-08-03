using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Evaluator
{
    public record TestCase(string Question, string GroundTruth);

    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Setup the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") 
                ?? throw new ArgumentNullException("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // Initialize our clean, single responsible agents
            var targetAgent = new TargetAgent(chatService);
            var judgeAgent = new JudgeAgent(chatService);

            // Define our automated test cases
            var tests = new[]
            {
                new TestCase(Question: "What is the captial of France?",
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

                // Judge evaluates the asnwer against the Ground Truth
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
