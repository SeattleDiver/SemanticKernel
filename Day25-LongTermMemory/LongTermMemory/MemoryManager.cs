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

        /// <summary>The currently loaded set of known facts about the user.</summary>
        public UserMemory CurrentMemory { get; private set;  }

        /// <summary>Creates a manager that loads existing memory (if any) from the given file path.</summary>
        /// <param name="chatService">The chat completion service used for fact extraction.</param>
        /// <param name="filePath">The JSON file path memory is persisted to and loaded from.</param>
        public MemoryManager(IChatCompletionService chatService, string filePath = "user_memory.json")
        {
            _chatService = chatService;
            _filePath = filePath;
            CurrentMemory = LoadMemory();
        }

        /// <summary>Reads the memory file from disk if it exists, falling back to a fresh, empty <see cref="UserMemory"/> on first run or corruption.</summary>
        /// <returns>The loaded (or freshly created) <see cref="UserMemory"/>.</returns>
        private UserMemory LoadMemory()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    return JsonSerializer.Deserialize<UserMemory>(json) ?? new UserMemory();
                }
                catch (JsonException)
                {
                    Console.WriteLine($"   [MEMORY MANAGER] Warning: '{_filePath}' is corrupted or unreadable; starting with fresh memory.");
                    return new UserMemory();
                }
            }
            return new UserMemory();
        }

        /// <summary>
        /// Analyzes the user input in the background to determine if a new fact should be saved.
        /// </summary>
        /// <param name="userInput">The user's latest message to analyze for a durable personal fact.</param>
        public async Task ExtractAndSaveFactAsync(string userInput)
        {
            // Prompt engineered specifically for data extraction, ignoring conversational filler.
            // The extraction rules are the system message; the actual text to analyze is a
            // separate user message - a ChatHistory containing only a system message has
            // nothing for the model to respond to, and some providers (Gemini among them)
            // reject it outright.
            string systemPrompt = @"
                Analyze the user's input.  If they state a permanent fact about themselves
                (e.g. their name, preferences, job, family), extract it as a short sentence.
                If there are no new personal facts, output exactly 'NONE'.
            ";

            var history = new ChatHistory(systemPrompt);
            history.AddUserMessage(userInput);
            var response = await _chatService.GetChatMessageContentAsync(history);
            string fact = response.Content?.Trim() ?? "NONE";

            // Only update the file if a valid, non-empty fact was found. A blank/
            // whitespace-only completion (e.g. a truncated or filtered response)
            // isn't "NONE" by string comparison, so it needs its own guard here.
            if (!string.IsNullOrWhiteSpace(fact) && !fact.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            {
                CurrentMemory.Facts.Add(fact);
                SaveMemory();
                Console.WriteLine($"\n   [MEMORY MANAGER] New fact saved: {fact}");
            }
        }

        /// <summary>Persists the current in-memory fact list back to the JSON file, overwriting it entirely each time.</summary>
        private void SaveMemory()
        {
            string json = JsonSerializer.Serialize(CurrentMemory, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
