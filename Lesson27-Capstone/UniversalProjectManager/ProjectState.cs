namespace UniversalProjectManager
{
    /// <summary>The shared, mutable "blackboard" state every agent in the pipeline reads from and writes to.</summary>
    internal class ProjectState
    {
        /// <summary>The user's original, unrefined project idea.</summary>
        public string OriginalRequest { get; set; } = string.Empty;

        /// <summary>The one-sentence technical goal produced by <see cref="GoalRefinerAgent"/>.</summary>
        public string RefinedGoal { get; set; } = string.Empty;

        /// <summary>The tasks the project has been broken down into.</summary>
        public List<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();

        /// <summary>Whether every task exists and has been marked complete.</summary>
        public bool IsFullyCompleted => Tasks.Any() && Tasks.All(t => t.IsCompleted);
    }
}
