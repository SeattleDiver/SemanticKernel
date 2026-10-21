// Day 13: Prompt Logic Plugins
// ---------------------------------------------------------------------------
// Shows how to keep looping/conditional logic OUT of the prompt template
// string by delegating it to plugin function calls instead
// ({{UserHelperPlugin.GetPurchaseList $user}} etc.). This keeps the template
// itself simple (just variable substitution) while the actual "if" and
// "loop" logic lives in ordinary, testable C# methods.
using System;
using System.Collections.Generic;
using Microsoft.SemanticKernel;

namespace PromptLogicPlugins
{
    /// <summary>Entry point that renders a customer-support prompt whose loop/conditional logic lives in plugin functions.</summary>
    class Program
    {
        /// <summary>Registers the logic plugin, then renders and sends a support prompt for a sample Gold-tier user.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);

            // Register the plugin that handles our logic/looping
            builder.Plugins.AddFromType<UserHelperPlugin>();

            Kernel kernel = builder.Build();

            var user = new UserProfile("Alice", "Gold", new List<Product> {
                new Product("AI Camera", 299.99),
                new Product("Tripod", 49.50)
            });

            // SHOWCASE: 
            // 1. We pass 'name' and 'tier' as simple strings (no dots).
            // 2. We call a plugin function to handle the complex 'loop' for purchases.
            // 3. We call a plugin function to handle the conditional 'if' logic for the tier.
            string promptTemplate = """
            System: 
            You are a customer support agent.
            {{UserHelperPlugin.GetTierInstructions $tier}}

            User Context:
            Name: {{$name}}
            Membership: {{$tier}}

            Recent Purchases:
            {{UserHelperPlugin.GetPurchaseList $user}}

            User Question: {{$input}}
            """;

            var arguments = new KernelArguments
            {
                ["user"] = user,       // Passed for the loop function
                ["name"] = user.Name,  // Passed for direct display
                ["tier"] = user.Tier,  // Passed for display and logic function
                ["input"] = "I need help with my recent order."
            };

            Console.WriteLine($"--- Processing {user.Name} via a Custom Native Plugin ---");

            // Step: Execute the prompt. Wrapped in try/catch because this one call
            // does double duty - it renders the template (which invokes
            // GetPurchaseList/GetTierInstructions) AND sends the request to the
            // model, so a plugin error, a bad key, or a network blip would
            // otherwise crash the whole program with a raw stack trace.
            try
            {
                var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

                Console.WriteLine("\nAI Response:");
                Console.WriteLine(result.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Request failed: {ex.Message}");
            }
        }
    }
}