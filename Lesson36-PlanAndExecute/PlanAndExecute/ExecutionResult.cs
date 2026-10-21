namespace PlanAndExecute
{
    /// <summary>
    /// Outcome of running an <see cref="ExecutionPlan"/> through the simulated environment.
    /// </summary>
    internal class ExecutionResult
    {
        /// <summary>Whether every step in the plan executed without hitting a blocking constraint.</summary>
        public bool Success { get; init; }

        /// <summary>A human-readable success summary, or the reason the plan failed.</summary>
        public string Message { get; init; } = string.Empty;
    }
}
