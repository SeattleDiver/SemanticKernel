namespace UniversalProjectManager
{
    public class ProjectState
    {
        public string OriginalRequest { get; set; } =string.Empty;
        public string RefinedGoal { get; set; } = string.Empty;
        public List<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
        public bool IsFullyCompleted => Tasks.Any() && Tasks.All(t => t.IsCompleted);
    }
}
