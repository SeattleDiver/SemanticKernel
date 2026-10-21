using Microsoft.SemanticKernel;
using System;
using System.ComponentModel;

namespace ManualReActLoop
{
    /// <summary>Stateful native plugin exposing a mock knowledge-base search and a running count of research steps taken.</summary>
    public class ResearchPlugin
    {
        private int _stepCount = 0;

        /// <summary>Searches the internal knowledge base for technical specs matching the query.</summary>
        /// <param name="query">The search query.</param>
        /// <returns>The matching spec, or "Data not found." if the query doesn't match the mock knowledge base.</returns>
        [KernelFunction("SearchDatabase")]
        [Description("Searches the internal knowledge base for technical specs.")]
        public string Search(string query)
        {
            _stepCount++;
            return query.Contains("Battery", StringComparison.OrdinalIgnoreCase) ? "The Quantum-X battery lasts 24 hours." : "Data not found.";
        }

        /// <summary>Returns how many research steps have been taken so far.</summary>
        /// <returns>A message stating the total number of <see cref="Search"/> calls made.</returns>
        [KernelFunction("GetStepCount")]
        [Description("Returns how many research steps have been taken.")]
        public string GetCount() => $"Total steps taken: {_stepCount}";
    }
}
