namespace UniversalProjectManager
{
    internal class ProjectTask
    {
        public string Id { get; set;  } = Guid.NewGuid().ToString("N").Substring(0,8);
        public string Description { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty; // e.g., "Developer", "Reviewer"
        // Step: default to empty string (matches sibling properties) so this satisfies
        // the project's <Nullable>enable</Nullable> setting before the Developer runs.
        public string Result { get; set; } = string.Empty;
        public bool IsCompleted { get; set; } = false;
    }
}
