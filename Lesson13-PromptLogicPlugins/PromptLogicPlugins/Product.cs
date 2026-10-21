namespace PromptLogicPlugins
{
    /// <summary>A single purchased item: its display name and price.</summary>
    /// <param name="Name">The product's display name.</param>
    /// <param name="Price">The price paid for the product.</param>
    public record Product(string Name, double Price);
}
