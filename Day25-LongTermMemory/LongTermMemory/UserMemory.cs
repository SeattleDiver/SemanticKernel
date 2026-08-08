// UserMemory
// ---------------------------------------------------------------------------
// The on-disk shape of an agent's long-term memory - a flat list of short
// facts, serialized to/from user_memory.json by MemoryManager.
using System.Text.Json.Serialization;

namespace LongTermMemory
{
    /// <summary>
    /// The data structure representing the agent's long-term storage.
    /// </summary>
    internal class UserMemory
    {
        /// <summary>The flat list of durable facts learned about the user so far.</summary>
        [JsonPropertyName("facts")]
        public List<string> Facts { get; set; } = new List<string>();
    }
}
