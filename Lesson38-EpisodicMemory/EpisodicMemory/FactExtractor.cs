using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace EpisodicMemory
{
    /// <summary>
    /// Decides whether a conversational turn contains a durable fact worth remembering.
    /// </summary>
    internal class FactExtractor
    {
        private readonly IChatCompletionService _chatService;

        public FactExtractor(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Returns the extracted fact (restated concisely), or null if the message contained
        /// nothing durable worth storing.
        /// </summary>
        public async Task<string?> ExtractAsync(string turnText)
        {
            string prompt = $$"""
                A user said the following in conversation. If it contains a durable fact about
                them worth remembering for future conversations (preferences, people, pets, job,
                plans, allergies, etc. - not small talk), restate that fact concisely in third
                person. Otherwise, use an empty string.

                USER MESSAGE:
                {{turnText}}

                Output ONLY valid JSON matching this schema:
                {
                    "fact": "the restated fact, or an empty string if there is none"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                ResponseFormat = typeof(FactExtractionResult)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                var result = JsonSerializer.Deserialize<FactExtractionResult>(response.Content ?? "{}");
                return string.IsNullOrWhiteSpace(result?.Fact) ? null : result.Fact;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
