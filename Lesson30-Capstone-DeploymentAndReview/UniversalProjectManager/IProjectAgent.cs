namespace UniversalProjectManager
{
    /// <summary>The minimal contract every specialist agent (GoalRefiner, Planner, Developer, Reviewer) must implement.</summary>
    internal interface IProjectAgent
    {
        /// <summary>This agent's display name.</summary>
        string Name { get; }

        /// <summary>Performs this agent's work against the shared project state, reading from and writing to it as needed.</summary>
        /// <param name="state">The shared blackboard state.</param>
        Task ExecuteAsync(ProjectState state);
    }
}
