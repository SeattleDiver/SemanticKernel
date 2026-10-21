using System;
using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace MultiToolAgent
{
    /// <summary>Native plugin exposing the current local time as a Semantic Kernel tool.</summary>
    public class TimePlugin
    {
        /// <summary>Gets the current local time.</summary>
        /// <returns>The current local date and time, formatted as a full date/time string.</returns>
        [KernelFunction("GetLocalTime")]
        [Description("Gets the current local time.")]
        public string GetTime()
        {
            Console.WriteLine("[EXECUTING TOOL] GetLocalTime.");
            return DateTime.Now.ToString("F");
        }
    }
}
