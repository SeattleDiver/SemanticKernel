using Microsoft.SemanticKernel.ChatCompletion;

namespace LongTermMemory
{
    internal class MemoryAgent
    {
        private readonly IChatCompletionService _chatService;
        private readonly ChatHistory _history;

        public MemoryAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
            _history = new ChatHistory();
        }

        public async Task<string> ChatAsync(string userInput, UserMemory memory)
        {
            // 1. Build the memroy context dynamically based on the current state of the JSON file.
            string memoryContext = memory.Facts.Count > 0
                ? string.Join("; ", memory.Facts)
                : "No facts known yet.";

            string systemMessage = $"You are a helpful personal assistant.  Use the following known facts about the user to personalize your response:\n{memoryContext}";

            // 2. Inject the Persona and Memory at index 0 (Using Fully Qualified Names to fix the CS error)
            _history.Insert(0, new Microsoft.SemanticKernel.ChatMessageContent(Microsoft.SemanticKernel.ChatCompletion.AuthorRole.System, systemMessage));
            _history.AddUserMessage(userInput);

            // 3. Generate the response
            var response = await _chatService.GetChatMessageContentAsync(_history);

            // 4. Clean up the system message to prevent context pollution on the next turn
            _history.RemoveAt(0);

            string aiResponse = response.Content ?? string.Empty;
            _history.AddAssistantMessage(aiResponse);

            return aiResponse;
        }
    }
}
