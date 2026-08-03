namespace UniversalProjectManager
{
    internal class ProjectTask
    {
        public string Id { get; set;  } = Guid.NewGuid().ToString("N").Substring(0,8);
        public string Description { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty; // e.g., "Developer", "Reviewer"
        public string Result { get; set; }
        public bool IsCompleted { get; set; } = false;
    }
}
