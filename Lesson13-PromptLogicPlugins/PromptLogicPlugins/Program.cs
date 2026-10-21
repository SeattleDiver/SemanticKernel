// Day 13: Prompt Logic Plugins
// ---------------------------------------------------------------------------
// Shows how to keep looping/conditional logic OUT of the prompt template
// string by delegating it to plugin function calls instead
// ({{UserHelperPlugin.GetPurchaseList $user}} etc.). This keeps the template
// itself simple (just variable substitution) while the actual "if" and
// "loop" logic lives in ordinary, testable C# methods.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;

namespace PromptLogicPlugins
{
    /// <summary>A single purchased item: its display name and price.</summary>
    public record Product(string Name, double Price);

    /// <summary>A customer's profile: display name, membership tier, and recent purchase history.</summary>
    public record UserProfile(string Name, string Tier, List<Product> RecentPurchases);

    // This plugin acts as your "Logic Layer" for the template
    /// <summary>Native plugin holding the looping/conditional logic kept out of the prompt template itself.</summary>
    public class UserHelperPlugin
    {
        /// <summary>Formats a user's recent purchases as a bulleted list for the prompt.</summary>
        /// <param name="user">The user whose purchase history should be formatted.</param>
        /// <returns>A newline-separated bullet list of purchases, or a message noting there are none.</returns>
        [KernelFunction]
        [Description("Formats the purchase list for the prompt")]
        public string GetPurchaseList(UserProfile user)
        {
            if (user.RecentPurchases == null || !user.RecentPurchases.Any())
                return "No recent purchases.";

            return string.Join("\n", user.RecentPurchases.Select(p => $"- {p.Name} (${p.Price})"));
        }

        /// <summary>Determines the tone and discount code to use based on the customer's membership tier.</summary>
        /// <param name="tier">The customer's membership tier (e.g. "Gold").</param>
        /// <returns>A tone/discount instruction string for the model to follow.</returns>
        [KernelFunction]
        [Description("Determines the tone and discount based on tier")]
        public string GetTierInstructions(string tier)
        {
            // No quote characters in these strings: SK's default template rendering
            // HTML-encodes function return values, which would turn 'GOLD20' into
            // the literal text &#39;GOLD20&#39; in the prompt actually sent to the model.
            return tier.Equals("Gold", StringComparison.OrdinalIgnoreCase)
                ? "Tone: Ultra-polite. Offer 20% discount code GOLD20."
                : "Tone: Professional. Offer 5% discount code SAVE5.";
        }
    }

    /// <summary>Entry point that renders a customer-support prompt whose loop/conditional logic lives in plugin functions.</summary>
    class Program
    {
        /// <summary>Registers the logic plugin, then renders and sends a support prompt for a sample Gold-tier user.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Missing Key");

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

            Console.WriteLine($"--- Processing {user.Name} via Built-in Plugins ---");

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