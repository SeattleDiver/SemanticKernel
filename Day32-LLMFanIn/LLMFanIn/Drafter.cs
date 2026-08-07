using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace LLMFanIn
{
    internal class Drafter
    {
        private readonly IChatCompletionService _chatService;

        public Drafter(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<string> GenerateDraftAsync(string task)
        {
            var history = new ChatHistory(
                "You are a technical writer. Answer the task directly, with no commentary.");
            history.AddUserMessage(task);

            var settings = new GeminiPromptExecutionSettings { Temperature = 0.9 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? string.Empty;
        }
    }
}
