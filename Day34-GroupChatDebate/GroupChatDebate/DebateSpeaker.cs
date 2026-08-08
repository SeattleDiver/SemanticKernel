using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace GroupChatDebate
{
    /// <summary>
    /// Generates one debate participant's turn from a fresh, fully-specified prompt each call.
    /// </summary>
    internal class DebateSpeaker
    {
        private readonly IChatCompletionService _chatService;

        public DebateSpeaker(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Produces the given participant's response to the debate transcript so far.
        /// </summary>
        public async Task<string> RespondAsync(DebateParticipant participant, string transcript)
        {
            var history = new ChatHistory(participant.Instructions);

            string prompt = transcript.Length == 0
                ? "The debate is starting. Make your opening point in 2-3 sentences."
                : $"TRANSCRIPT SO FAR:\n{transcript}\n\nIt's your turn. Respond directly to what was just said, in 2-3 sentences.";

            history.AddUserMessage(prompt);

            var settings = new GeminiPromptExecutionSettings { Temperature = 0.7 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? string.Empty;
        }
    }
}
