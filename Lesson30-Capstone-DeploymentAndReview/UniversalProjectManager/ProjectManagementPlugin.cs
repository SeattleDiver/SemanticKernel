using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace UniversalProjectManager
{
    /// <summary>State-aware plugin exposing project-mutation tools that close over the same shared ProjectState every agent reads.</summary>
    internal class ProjectManagementPlugin
    {
        private readonly ProjectState _state;

        /// <summary>Creates a plugin that mutates the given shared project state.</summary>
        /// <param name="state">The shared blackboard state new tasks are added to.</param>
        public ProjectManagementPlugin(ProjectState state)
        {
            _state = state;
        }

        /// <summary>Creates a new actionable task for the project and assigns it to a specialist role.</summary>
        /// <param name="description">Detailed description of the task.</param>
        /// <param name="assignedTo">The role responsible, e.g. "Developer", "Reviewer", "Tester".</param>
        /// <returns>A confirmation message including the new task's generated ID.</returns>
        [KernelFunction("CreateTask")]
        [Description("Creates a new actionable task for the project and assigns it to a specialist")]
        public string CreateTask(
            [Description("Detailed description of the task")] string description,
            [Description("The role responsible (e.g. 'Developer', 'Reviewer', 'Tester')")] string assignedTo
        )
        {
            var task = new ProjectTask
            {
                Description = description,
                AssignedTo = assignedTo,
            };

            _state.Tasks.Add(task);

            Console.WriteLine($"  [TOOL EXECUTED] Task created: {description.Substring(0, Math.Min(30, description.Length))}...{assignedTo}");
         
            return $"Task successfully created with ID {task.Id} and assigned to {assignedTo}.";
        }
    }
}
