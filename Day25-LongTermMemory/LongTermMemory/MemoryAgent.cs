using Microsoft.SemanticKernel.ChatCompletion;

namespace LongTermMemory
{
    /// <summary>The foreground conversationalist: chats with the user, injecting known facts as context each turn.</summary>
    internal class MemoryAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly ChatHistory _history;

        /// <summary>Creates an agent that chats using the given chat service, starting from an empty history.</summary>
        /// <param name="chatService">The chat completion service used to generate responses.</param>
        public MemoryAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
            _history = new ChatHistory();
        }

        /// <summary>Responds to the user's input, using the current known facts as personalization context for this turn only.</summary>
        /// <param name="userInput">The user's message.</param>
        /// <param name="memory">The currently known facts about the user.</param>
        /// <returns>The agent's response.</returns>
        public async Task<string> ChatAsync(string userInput, UserMemory memory)
        {
            // 1. Build the memory context dynamically based on the current state of the JSON file.
            string memoryContext = memory.Facts.Count > 0
                ? string.Join("; ", memory.Facts)
                : "No facts known yet.";

            string systemMessage = $"You are a helpful personal assistant.  Use the following known facts about the user to personalize your response:\n{memoryContext}";

            // 2. Inject the Persona and Memory at index 0 (Using Fully Qualified Names to fix the CS error)
            _history.Insert(0, new Microsoft.SemanticKernel.ChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System, systemMessage));
            _history.AddUserMessage(userInput);

            Microsoft.SemanticKernel.ChatMessageContent response;
            try
            {
                // 3. Generate the response
                response = await _chatService.GetChatMessageContentAsync(_history);
            }
            catch
            {
                // Roll back both the system persona and the now-orphaned user turn so a
                // failed call doesn't leave history corrupted for the next attempt - the
                // persona would otherwise stay stuck at index 0, and the unanswered user
                // turn would get resent on every later call.
                _history.RemoveAt(0);
                _history.RemoveAt(_history.Count - 1);
                throw;
            }

            // 4. Clean up the system message to prevent context pollution on the next turn
            _history.RemoveAt(0);

            string aiResponse = response.Content ?? string.Empty;
            _history.AddAssistantMessage(aiResponse);

            return aiResponse;
        }
    }
}
