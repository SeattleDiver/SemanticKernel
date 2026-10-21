namespace PlanAndExecute
{
    /// <summary>
    /// A deterministic stand-in for real infrastructure. Enforces one undocumented constraint -
    /// a schema migration step fails unless an earlier step has paused the archiver job - so the
    /// planner only learns about it from a genuine execution failure, never from the goal text.
    /// </summary>
    internal static class SimulatedDeploymentEnvironment
    {
        /// <summary>
        /// Executes the plan's steps in order, stopping at the first step that violates the
        /// environment's constraint.
        /// </summary>
        public static ExecutionResult RunPlan(ExecutionPlan plan)
        {
            bool archiverPaused = false;

            foreach (string step in plan.Steps)
            {
                string lower = step.ToLowerInvariant();

                if (lower.Contains("pause") && lower.Contains("archiver"))
                {
                    archiverPaused = true;
                    continue;
                }

                if ((lower.Contains("migrat") || lower.Contains("schema")) && !archiverPaused)
                {
                    return new ExecutionResult
                    {
                        Success = false,
                        Message =
                            $"Step failed: \"{step}\". Migration failed: ArchiverJob is currently active " +
                            "and holds a lock on the orders table. ArchiverJob must be paused before any " +
                            "schema change step runs."
                    };
                }
            }

            return new ExecutionResult { Success = true, Message = "All steps executed successfully." };
        }
    }
}
