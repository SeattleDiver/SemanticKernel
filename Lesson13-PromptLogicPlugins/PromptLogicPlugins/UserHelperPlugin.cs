using System;
using System.ComponentModel;
using System.Linq;
using Microsoft.SemanticKernel;

namespace PromptLogicPlugins
{
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
}
