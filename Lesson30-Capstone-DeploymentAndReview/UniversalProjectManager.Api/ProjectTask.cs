namespace UniversalProjectManager
{
    /// <summary>A single unit of work on the project blackboard.</summary>
    public class ProjectTask
    {
        /// <summary>A short, unique identifier for this task.</summary>
        public string Id { get; set;  } = Guid.NewGuid().ToString("N").Substring(0,8);

        /// <summary>A human-readable description of what this task involves.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>Which agent role owns this task, e.g. "Developer", "Reviewer".</summary>
        public string AssignedTo { get; set; } = string.Empty;

        /// <summary>The output produced by whichever agent completed this task, if any.</summary>
        public string Result { get; set; } = string.Empty; // Step: default like its sibling properties to avoid a null-reference at runtime

        /// <summary>Whether this task has been completed.</summary>
        public bool IsCompleted { get; set; } = false;
    }
}
