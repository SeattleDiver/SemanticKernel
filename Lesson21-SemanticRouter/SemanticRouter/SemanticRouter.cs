// SemanticRouter
// ---------------------------------------------------------------------------
// The "switchboard": classifies a user's intent (TECH/BILLING/GENERAL) into
// strict JSON via ResponseFormat, so the caller can deserialize the
// decision instead of pattern-matching free text. The prompt asks for
// "reasoning" before "intent" deliberately - forcing a chain-of-thought
// explanation first measurably improves classification accuracy.
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace SemanticRouter
{
    /// <summary>The "switchboard": classifies free-text user input into TECH/BILLING/GENERAL via strict JSON output.</summary>
    internal class SemanticRouter
    {
        private readonly IChatCompletionService _chatService;
        private readonly string _routerPrompt;

        /// <summary>Creates a router that classifies intent using the given chat service.</summary>
        /// <param name="chatService">The chat completion service used to classify intent.</param>
        public SemanticRouter(IChatCompletionService chatService)
        {
            _chatService = chatService;

            _routerPrompt = @"
                You are a highly efficient Semantic Router.
                Analyze the user's input and classify their intent into one of three categories:
                - TECH: The user needs technical support, password resets, or system troubleshooting.
                - BILLING: The user is asking about invoices, account balances, or payments.
                - GENERAL: Anything else (small talk, weather, etc.).

                You MUST output valid JSON matching this schema:
                {
                    ""reasoning"": ""Briefly explain why you chose this category"",
                    ""intent"": ""TECH"" || ""BILLING"" || ""GENERAL""
                }
            ";
        }

        /// <summary>Classifies a user's input into a routing decision (TECH, BILLING, or GENERAL) via strict JSON output.</summary>
        /// <param name="userInput">The free-text user request to classify.</param>
        /// <returns>The parsed <see cref="RouteDecision"/>, or a GENERAL fallback if the model's JSON couldn't be parsed.</returns>
        public async Task<RouteDecision> RouteAsync(string userInput)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(_routerPrompt);
            history.AddUserMessage(userInput);

            // Force the model to output structured JSON deterministically
            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                Temperature = 0.0
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            // Step: Malformed/truncated JSON should never crash the console loop -
            // fall back to GENERAL, the same safe default used for a null result below.
            try
            {
                return JsonSerializer.Deserialize<RouteDecision>(response.Content ?? "{}")
                    ?? new RouteDecision { Intent = "GENERAL" };
            }
            catch (JsonException)
            {
                return new RouteDecision { Intent = "GENERAL", Reasoning = "Router returned unparseable JSON; defaulted to GENERAL." };
            }
        }
    }
}
