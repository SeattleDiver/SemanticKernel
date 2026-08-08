using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace VotingEnsembles
{
    /// <summary>
    /// Draws one independent, high-temperature reasoning attempt at a given problem.
    /// </summary>
    internal class ReasoningSampler
    {
        private readonly IChatCompletionService _chatService;

        public ReasoningSampler(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Solves the given question and returns a structured answer plus brief reasoning.
        /// </summary>
        public async Task<ReasoningSample> SampleAsync(string question)
        {
            string prompt = $$"""
                Solve the problem below. Work through it step by step internally, then report
                your answer - many people get this kind of problem wrong by skipping the steps.

                PROBLEM:
                {{question}}

                Output ONLY valid JSON matching this schema:
                {
                    "finalAnswer": "The final answer only, as a short exact value (e.g. 0.05), with no extra words.",
                    "reasoning": "Your reasoning, in at most 3 concise sentences."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.8,
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(ReasoningSample)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<ReasoningSample>(response.Content ?? "{}")
                    ?? Unparseable();
            }
            catch (JsonException)
            {
                return Unparseable();
            }
        }

        /// <summary>
        /// Fallback sample used when this attempt's own JSON response can't be parsed.
        /// </summary>
        private static ReasoningSample Unparseable() => new()
        {
            Reasoning = "Failed to parse this sample's JSON response.",
            FinalAnswer = "UNPARSEABLE"
        };
    }
}
