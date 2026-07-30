using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Evaluator
{
    internal class TargetAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly string _persona;

        public TargetAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
            _persona = "You are a customer support bot.  Answer the user's question briefly.";
        }

        public async Task<string> AskQuestionAsync(string question)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(_persona);
            history.AddUserMessage(question);

            // We use a slight temperature here to allow for natural conversation
            var settings = new GeminiPromptExecutionSettings { Temperature = 0.4 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? string.Empty;
        }
    }
}
