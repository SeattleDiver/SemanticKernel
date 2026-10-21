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
        /// <param name="participant">The team submitting the bid.</param>
        /// <param name="totalBudget">The shared budget all teams are competing for.</param>
        /// <param name="marketSignal">The public summary of the previous round shown to every team.</param>
        /// <returns>The participant's parsed bid for this round.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the reply is empty or can't be parsed as a bid.</exception>
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

            // An unparseable reply is not a $0 bid. Throwing lets the caller treat it exactly like
            // a failed call, so a bad parse can never make the total "fit" the budget.
            Bid? bid;
            try
            {
                bid = string.IsNullOrWhiteSpace(response.Content)
                    ? null
                    : JsonSerializer.Deserialize<Bid>(response.Content);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException("Bid response could not be parsed.", ex);
            }

            return bid ?? throw new InvalidOperationException("Bid response was empty.");
        }
    }
}
