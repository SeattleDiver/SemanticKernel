using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Microsoft.SemanticKernel;

namespace BuiltInAgent
{
    public record Product(string Name, double Price);
    public record UserProfile(string Name, string Tier, List<Product> RecentPurchases);

    // This plugin acts as your "Logic Layer" for the template
    public class UserHelperPlugin
    {
        [KernelFunction]
        [Description("Formats the purchase list for the prompt")]
        public string GetPurchaseList(UserProfile user)
        {
            if (user.RecentPurchases == null || !user.RecentPurchases.Any())
                return "No recent purchases.";

            return string.Join("\n", user.RecentPurchases.Select(p => $"- {p.Name} (${p.Price})"));
        }

        [KernelFunction]
        [Description("Determines the tone and discount based on tier")]
        public string GetTierInstructions(string tier)
        {
            return tier.Equals("Gold", StringComparison.OrdinalIgnoreCase)
                ? "Tone: Ultra-polite. Offer 20% discount code 'GOLD20'."
                : "Tone: Professional. Offer 5% discount code 'SAVE5'.";
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("Missing Key");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

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

            var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

            Console.WriteLine("\nAI Response:");
            Console.WriteLine(result.ToString());
        }
    }
}