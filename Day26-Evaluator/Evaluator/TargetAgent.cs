using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Evaluator
{
    /// <summary>The agent under test: a simple customer-support bot whose answers the JudgeAgent will score.</summary>
    internal class TargetAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly string _persona;

        /// <summary>Creates a target agent that answers questions as a customer-support bot.</summary>
        /// <param name="chatService">The chat completion service used to generate answers.</param>
        public TargetAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
            _persona = "You are a customer support bot.  Answer the user's question briefly.";
        }

        /// <summary>Answers a single question under the target agent's persona.</summary>
        /// <param name="question">The question to answer.</param>
        /// <returns>The agent's response.</returns>
        public async Task<string> AskQuestionAsync(string question)
        {
            var history = new ChatHistory();
            history.AddSystemMessage(_persona);
            history.AddUserMessage(question);

            // We use a slight temperature here to allow for natural conversation
            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.4 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? string.Empty;
        }
    }
}
