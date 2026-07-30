// UserMemory
// ---------------------------------------------------------------------------
// The on-disk shape of an agent's long-term memory - a flat list of short
// facts, serialized to/from user_memory.json by MemoryManager.
using System.Text.Json.Serialization;

namespace LongTermMemory
{
    internal class UserMemory
    {
        /// <summary>
        /// The data structure representing the agetn's long-term storage.
        /// </summary>
        [JsonPropertyName("facts")]
        public List<string> Facts { get; set; } = new List<string>();
    }
}
