using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;
using System.ComponentModel;

namespace Calculator
{

    public class Program
    {
        static async Task Main(string[] args)
        {
            // Step 2: Init the kernel
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable not set");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);

            // Step 3: Load the native plug in
            builder.Plugins.AddFromType<MathPlugIn>("Math");

            Kernel kernel = builder.Build();

            // Step 4: Math word problem
            string prompt = "I had 124 apples.  I ate 15 of them.  Then I multiplied the amount of apples I had left by 3.  Finally, I divided all my apples equally among myself and 3 friends.  How many apples did each person get?  Do all math using my tools.  Show your work step by step.";

            // Step 5: Enable Auto-Invocation of the tools
            var executionSettings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
            };

            var arguments = new KernelArguments(executionSettings);
            Console.WriteLine($"Question: {prompt}");
            Console.WriteLine("Thinking and invoking C# tools...\n");

            // Step 6: Run the prompt
            var result = await kernel.InvokePromptAsync(prompt, arguments);
            
            // Step 7: Display the final result
            Console.WriteLine("\n --- AI response ---"); 
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("-------------------");
        }

    }
}