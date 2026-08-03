using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace UniversalProjectManager
{
    internal class ProjectManagementPlugin
    {
        private readonly ProjectState _state;

        public ProjectManagementPlugin(ProjectState state)
        {
            _state = state;
        }

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
