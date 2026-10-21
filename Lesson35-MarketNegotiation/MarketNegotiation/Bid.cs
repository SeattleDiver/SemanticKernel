using System.Text.Json.Serialization;

namespace MarketNegotiation
{
    /// <summary>
    /// Structured result returned by <see cref="BidNegotiator.SubmitBidAsync"/>.
    /// </summary>
    internal class Bid
    {
        /// <summary>The amount this participant is asking for in this round.</summary>
        [JsonPropertyName("requestedAmount")]
        public decimal RequestedAmount { get; set; }

        /// <summary>A short justification for the requested amount.</summary>
        [JsonPropertyName("justification")]
        public string Justification { get; set; } = string.Empty;
    }
}
