using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HumanInTheLoop
{
    /// <summary>
    /// Encapsulates the AI Persona and the Native Orchestration logic required to communicate with Gemini
    /// </summary>
    internal class AiWorker
    {
        private readonly IChatCompletionService _chatService;
        private readonly string _persona;

        public AiWorker(IChatCompletionService chatService, string persona)
        {
            _chatService = chatService;
            _persona = persona;
        }

        public async Task<string> GenerateDraftAsync(ChatHistory history)
        {
            // 1. Inject the System Perona at the top of the history
            var systemMessage = new ChatMessageContent(AuthorRole.System, _persona);
            history.Insert(0, systemMessage);

            // 2. enerate the draft using Gemini
            var result = await _chatService.GetChatMessageContentAsync(history);
            string draft = result.Content ?? "No draft generated.";

            // 3. Clean up the Persona to keep history pure, then append the AI's response
            history.RemoveAt(0);
            history.AddAssistantMessage(draft);
            
            return draft;
        }

    }
}
