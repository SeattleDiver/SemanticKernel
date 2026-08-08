using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace MarketNegotiation
{
    /// <summary>
    /// Generates one participant's bid for the current negotiation round.
    /// </summary>
    internal class BidNegotiator
    {
        private readonly IChatCompletionService _chatService;

        public BidNegotiator(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Submits a bid for the given participant, informed by the current market signal
        /// (e.g. how far the total ask is over budget and what everyone else is asking for).
        /// </summary>
        public async Task<Bid> SubmitBidAsync(NegotiationParticipant participant, decimal totalBudget, string marketSignal)
        {
            string prompt = $$"""
                {{participant.Instructions}}

                The shared budget for all teams combined is ${{totalBudget}}.
                {{marketSignal}}

                Submit your bid for this round. Be reasonable - an unreasonable bid that ignores
                the budget reality will not be respected in the final allocation.

                Output ONLY valid JSON matching this schema:
                {
                    "requestedAmount": number,
                    "justification": "one short sentence, at most 20 words"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.5,
                ResponseFormat = typeof(Bid)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<Bid>(response.Content ?? "{}") ?? Unparseable();
            }
            catch (JsonException)
            {
                return Unparseable();
            }
        }

        /// <summary>
        /// Fallback bid used when this participant's own JSON response can't be parsed.
        /// </summary>
        private static Bid Unparseable() => new()
        {
            RequestedAmount = 0,
            Justification = "Failed to parse bid response."
        };
    }
}
