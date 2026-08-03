using System;
using System.Collections.Generic;
using System.Text;

namespace UniversalProjectManager
{
    internal class ProjectOrchestrator
    {
        readonly List<IProjectAgent> _agents;

        public ProjectOrchestrator(IEnumerable<IProjectAgent> agents)
        {
            _agents = new List<IProjectAgent>(agents);
        }

        public async Task RunProjectAsync(ProjectState state)
        {
            int maxCycles = 5;
            int currentCycle = 0;

            Console.WriteLine("\n Orchestrator started workflow...");

            while (currentCycle < maxCycles)
            {
                Console.WriteLine($"\n---- ORCHESTRATION CYCLE {currentCycle + 1} ---");

                foreach(var agent in _agents)
                {
                    await agent.ExecuteAsync(state);
                }

                // If tasks exist and they are all completed (and survived the Reviewer), we are done.
                if (state.IsFullyCompleted)
                {
                    Console.WriteLine("\nProject successfully completed!");
                    return;
                }

                currentCycle++;
            }

            Console.WriteLine("\nOrchestrator halted: Maximum cycles reached without full cmpletion.");
        }
    }
}
