using System;
using System.Collections.Generic;

namespace UniversalProjectManager
{
    /// <summary>Owns the agent lifecycle: repeatedly runs every registered agent in order until the project converges or times out.</summary>
    public class ProjectOrchestrator
    {
        readonly List<IProjectAgent> _agents;

        /// <summary>Creates an orchestrator that runs the given agents, in order, once per cycle.</summary>
        /// <param name="agents">The agents to run each cycle, in registration order. In the Web API, DI supplies one of each registered <see cref="IProjectAgent"/>.</param>
        public ProjectOrchestrator(IEnumerable<IProjectAgent> agents)
        {
            _agents = new List<IProjectAgent>(agents);
        }

        /// <summary>Runs every agent against the shared state once per cycle until <see cref="ProjectState.IsFullyCompleted"/> or the cycle cap is reached.</summary>
        /// <param name="state">The shared project state passed to every agent each cycle.</param>
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

            Console.WriteLine("\nOrchestrator halted: Maximum cycles reached without full completion.");
        }
    }
}
