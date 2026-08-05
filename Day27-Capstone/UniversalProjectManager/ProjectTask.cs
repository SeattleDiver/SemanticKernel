namespace UniversalProjectManager
{
    internal class ProjectTask
    {
        public string Id { get; set;  } = Guid.NewGuid().ToString("N").Substring(0,8);
        public string Description { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty; // e.g., "Developer", "Reviewer"
        // Step: default to string.Empty like the other string properties above,
        // so a task that hasn't produced output yet is never null under nullable reference types.
        public string Result { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
    }
}
