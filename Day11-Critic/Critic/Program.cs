using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace critic
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in Critic.csproj) before building.");
#else
            // 1. Setup kernel with a chat model
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
#endif
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
#if CHATGPT
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                TopP = 0.1
            };
#elif GOOGLE
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
                TopP = 0.1
            };
#endif

            var arguments = new KernelArguments(executionSettings)
            {
                { "input", codeToReview }
            };

            Console.WriteLine("The critic is analyzing the code...\n");
            var result = await kernel.InvokePromptAsync(criticPrompt, arguments);

            Console.WriteLine("--- Critique Result ---");
            Console.WriteLine(result.ToString().Trim());
#endif
        }
    }
}