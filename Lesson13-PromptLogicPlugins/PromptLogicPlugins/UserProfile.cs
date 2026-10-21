using System.Collections.Generic;

namespace PromptLogicPlugins
{
    /// <summary>A customer's profile: display name, membership tier, and recent purchase history.</summary>
    /// <param name="Name">The customer's display name.</param>
    /// <param name="Tier">The customer's membership tier (e.g. "Gold").</param>
    /// <param name="RecentPurchases">The products the customer bought recently.</param>
    public record UserProfile(string Name, string Tier, List<Product> RecentPurchases);
}
