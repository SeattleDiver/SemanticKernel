// Day 4: The Calculator
// ---------------------------------------------------------------------------
// LLMs are prediction engines, not calculators - they're notoriously bad at
// precise arithmetic. This episode fixes that by wrapping real C# math in a
// native plugin (see MathPlugIn.cs) and enabling auto tool-invocation, so the
// model can pause generation, call our code for the exact answer, and resume.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;

namespace Calculator
{

    /// <summary>Entry point that wires a native math plugin into the kernel and lets the model call it to solve a word problem.</summary>
    public class Program
    {
        /// <summary>Registers the math plugin, enables auto tool-invocation, and asks the model to solve a multi-step word problem using it.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 2: Init the kernel
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable not set");
            string modelId = "gpt-4.1-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Step 3: Load the native plug in
            builder.Plugins.AddFromType<MathPlugin>("Math");

            Kernel kernel = builder.Build();

            // Step 4: Math word problem
            string prompt = "I had 124 apples.  I ate 15 of them.  Then I multiplied the amount of apples I had left by 3.  Finally, I divided all my apples equally among myself and 3 friends.  How many apples did each person get?  Do all math using my tools.  Show your work step by step.";

            // Step 5: Enable Auto-Invocation of the tools. Without this, the
            // model would know the Math plugin exists but wouldn't have
            // permission to call it - it would just try (and fail) to guess
            // the arithmetic itself.
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
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