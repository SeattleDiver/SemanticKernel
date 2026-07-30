// MemoryManager
// ---------------------------------------------------------------------------
// Owns the agent's persistent memory file: loads it on startup, uses a
// dedicated extraction prompt to decide whether a user's message contains a
// durable fact worth remembering, and saves it back to disk when it does -
// giving the agent memory that survives past the current process.
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace LongTermMemory
{
    /// <summary>
    /// Handles loading, saving, and extracting permanent facts from the conversation
    /// </summary>
    internal class MemoryManager
    {
        private readonly string _filePath;
        private readonly IChatCompletionService _chatService;

        public UserMemory CurrentMemory { get; private set;  }

        public MemoryManager(IChatCompletionService chatService, string filePath = "user_memory.json")
        {
            _chatService = chatService;
            _filePath = filePath;
            CurrentMemory = LoadMemory();
        }

        // Reads the memory file from disk if it exists; otherwise starts fresh
        // with an empty fact list rather than failing on first run.
        private UserMemory LoadMemory()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<UserMemory>(json);
            }
            return new UserMemory();
        }

        /// <summary>
        /// Analyzes the user input in the background to determin if a new fact should be saved
        /// </summary>
        public async Task ExtractAndSaveFactAsync(string userInput)
        {
            // Prompt engineered specifcally for data extraction, ignoring conversational filler.
            // The extraction rules are the system message; the actual text to analyze must be
            // a separate user message - a ChatHistory containing only a system message is
            // rejected by Gemini ("Chat history can't contain only system messages").
            string systemPrompt = @"
                Analyze the user's input.  If they state a permanent fact about themselves
                (e.g. their name, preferences, job, family), extract it as a short sentence.
                If there are no new personal facts, output exactly 'NONE'.
            ";

            var history = new ChatHistory(systemPrompt);
            history.AddUserMessage(userInput);
            var response = await _chatService.GetChatMessageContentAsync(history);
            string fact = response.Content?.Trim() ?? "NONE";

            // Only update the file if a valid fact was found
            if (!fact.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                CurrentMemory.Facts.Add(fact);
                SaveMemory();
                Console.WriteLine($"\n   [MEMORY MANAGER] New fact saved: {fact}");
            }
        }

        // Persists the current in-memory fact list back to the JSON file,
        // overwriting it entirely each time.
        private void SaveMemory()
        {
            string json = JsonSerializer.Serialize(CurrentMemory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
