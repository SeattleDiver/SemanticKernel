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
        /// environment's constraint. A plan with no steps is itself a failure.
        /// </summary>
        /// <param name="plan">The plan to execute.</param>
        /// <returns>A success result, or a failure result describing why execution stopped.</returns>
        public static ExecutionResult RunPlan(ExecutionPlan plan)
        {
            // An empty plan (e.g. the planner's reply couldn't be parsed) accomplishes nothing,
            // so it must not fall through to "All steps executed successfully."
            if (plan.Steps.Count == 0)
            {
                return new ExecutionResult
                {
                    Success = false,
                    Message =
                        "The plan contained no steps (the planner's response was empty or could not " +
                        "be parsed). Produce a complete, ordered plan for the goal."
                };
            }

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
