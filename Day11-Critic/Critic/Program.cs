// Day 11: The Critic
// ---------------------------------------------------------------------------
// A rubric-based code reviewer. Rather than forcing real structured JSON
// output (that comes later, in Days 19/21), this episode gets a structured-
// looking result purely through prompt instructions - asking the model to
// follow a fixed SCORE/PROS/CONS/FIX text format. The sample method under
// review has a deliberate bug (unchecked division) for the critic to catch.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace critic
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Setup kernel witih Gemini 2.5 flash
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            // 2. The code to be critiqued (output from day 10)
            string codeToReview = @"
public int Calculate(int a, int b)
{
    var result = a / b;
    return result;
}";

            // 3. Define the critic's rubric via a prompt template
            string criticPrompt = @"
System: You are a senior security auditor and Performance engineer.
Your task is to critique the C# code provided by the user.

Evaluate the code based on these 3 criteria:
1. Reliability (Is there error handling?)
2. Performane (Is it efficient?)
3. Best Practices (Does it follow C# best practices?)

Output your critique in this EXACT format:
SCORE: [0-10]
PROS: [List hightlights]
CONS: [List flaws]
FIX: [Provide a corrected version of the code if necessary]

User: Review this c# code: {{$input}}";

            // 4. Execute the Critique
            // We use the temperature 0.0 because a critic should be objective and consistent.
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
                TopP = 0.1
            };

            var arguments = new KernelArguments(executionSettings)
            {
                { "input", codeToReview }
            };

            Console.WriteLine("The critic is analyzing the code...\n");

            // 5. A live API call can fail (bad key, rate limit, network, content
            // safety rejection). Catch it so students get a clear message instead
            // of an unhandled exception crashing the whole session.
            try
            {
                var result = await kernel.InvokePromptAsync(criticPrompt, arguments);

                Console.WriteLine("--- Critique Result ---");
                Console.WriteLine(result.ToString().Trim());
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critique failed - see error below:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}