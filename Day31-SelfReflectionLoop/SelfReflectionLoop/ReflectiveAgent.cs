using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace SelfReflectionLoop
{
    internal class ReflectiveAgent
    {
        private readonly IChatCompletionService _chatService;

        public ReflectiveAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> DraftAsync(string task)
        {
            var history = new ChatHistory(
                "You are a skilled writer. Produce a first draft that satisfies the task exactly. " +
                "Output only the draft itself, with no commentary.");
            history.AddUserMessage(task);

            var settings = new GeminiPromptExecutionSettings { Temperature = 0.7 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? string.Empty;
        }

        public async Task<SelfCritique> CritiqueAsync(string task, string draft)
        {
            string prompt = $$"""
                You are the same writer, reviewing your own draft before it ships. Judge it
                strictly against the original task - do not invent requirements that were
                never asked for.

                ORIGINAL TASK:
                {{task}}

                YOUR DRAFT:
                {{draft}}

                Output ONLY valid JSON matching this schema:
                {
                    "isSatisfactory": true/false,
                    "feedback": "Specific, actionable feedback on what to fix. Empty string if satisfactory."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(SelfCritique)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<SelfCritique>(response.Content ?? "{}")
                    ?? new SelfCritique { IsSatisfactory = false, Feedback = "Failed to parse self-critique JSON." };
            }
            catch (JsonException)
            {
                return new SelfCritique { IsSatisfactory = false, Feedback = "Failed to parse self-critique JSON." };
            }
        }

        public async Task<string> ReviseAsync(string task, string draft, string feedback)
        {
            string prompt = $"""
                You are the same writer, revising your own draft based on your own critique.
                Output only the revised draft, with no commentary on what changed.

                ORIGINAL TASK:
                {task}

                PREVIOUS DRAFT:
                {draft}

                YOUR OWN FEEDBACK TO ADDRESS:
                {feedback}
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings { Temperature = 0.7 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? draft;
        }
    }
}
