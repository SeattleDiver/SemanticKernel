using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ChatResponseFormat = OpenAI.Chat.ChatResponseFormat;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif
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

            // Force structured JSON output deterministically
#if CHATGPT
            var settings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
                Temperature = 0.0
            };
#elif GOOGLE
            var settings = new GeminiPromptExecutionSettings
            {
                ResponseMimeType = "application/json",
                Temperature = 0.0
            };
#endif

            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return JsonSerializer.Deserialize<RouteDecision>(response.Content ?? "{}")
                ?? new RouteDecision { Intent = "GENERAL" };
        }
    }
}
