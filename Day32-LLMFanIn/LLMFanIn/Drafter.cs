using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    /// <summary>
    /// Generates one independent, high-temperature attempt at a given task.
    /// </summary>
    internal class Drafter
    {
        private readonly IChatCompletionService _chatService;

        public Drafter(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Produces a single free-text draft answering the given task.
        /// </summary>
        public async Task<string> GenerateDraftAsync(string task)
        {
            var history = new ChatHistory(
                "You are a technical writer. Answer the task directly, with no commentary.");
            history.AddUserMessage(task);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.9 };
            var result = await _chatService.GetChatMessageContentAsync(history, settings);
            return result.Content ?? string.Empty;
        }
    }
}
