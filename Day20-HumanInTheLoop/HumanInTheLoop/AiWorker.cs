// AiWorker
// ---------------------------------------------------------------------------
// Encapsulates the AI persona and the persona-swapping orchestration logic
// (inject system message, generate, remove system message) so Program.cs can
// stay a clean, high-level orchestration script.
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

            try
            {
                // 2. enerate the draft using Gemini
                var result = await _chatService.GetChatMessageContentAsync(history);
                string draft = result.Content ?? "No draft generated.";

                // 3. Append the AI's response now that the call succeeded
                history.AddAssistantMessage(draft);

                return draft;
            }
            finally
            {
                // Step: Always remove the temporary persona message, even if the
                // call above throws. Without this, a failed call would leave the
                // system message stuck at index 0 and corrupt every later attempt.
                history.RemoveAt(0);
            }
        }

    }
}
