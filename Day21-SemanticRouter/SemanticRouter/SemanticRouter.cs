// SemanticRouter
// ---------------------------------------------------------------------------
// The "switchboard": classifies a user's intent (TECH/BILLING/GENERAL) into
// strict JSON via ResponseMimeType, so the caller can deserialize the
// decision instead of pattern-matching free text. The prompt asks for
// "reasoning" before "intent" deliberately - forcing a chain-of-thought
// explanation first measurably improves classification accuracy.
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;

namespace SemanticRouter
{
    internal class SemanticRouter
    {
        private readonly IChatCompletionService _chatService;
        private readonly string _routerPrompt;

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

        public async Task<RouteDecision> RouteAsync(string userInput)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(_routerPrompt);
            history.AddUserMessage(userInput);

            // Force Gemini to output structured JSON deterministically
            var settings = new GeminiPromptExecutionSettings
            {
                ResponseMimeType = "application/json",
                Temperature = 0.0
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return JsonSerializer.Deserialize<RouteDecision>(response.Content ?? "{}")
                ?? new RouteDecision { Intent = "GENERAL" };
        }
    }
}
