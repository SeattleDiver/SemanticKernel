// SecureDatabasePlugin
// ---------------------------------------------------------------------------
// A plain, single-purpose plugin with zero logging/timing code - all of that
// cross-cutting concern lives in the filters instead, keeping this class
// focused only on its actual job.
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FunctionFilters
{
    /// <summary>
    /// A mock data plugin that the AI will autonomously decide to call.
    /// </summary>
    internal class SecureDatabasePlugin
    {
        /// <summary>Retrieves the account balance for a specific customer ID.</summary>
        /// <param name="customerId">The unique customer ID to look up.</param>
        /// <returns>The customer's simulated account balance.</returns>
        [KernelFunction("GetCustomerBalance")]
        [Description("Retrieves the account balance for a specific customer ID.")]
        public async Task<string> GetBalanceAsync([Description("The unique customer ID")]string customerId)
        {
            // Simulate a network/database delay
            await Task.Delay(400);

            //string response = $"Customer {customerId} has an active balance of $5,240";
            string response = "$5,240";
            Console.WriteLine($"[DATABASE PLUGIN]: {response}");
            return response;
        }
    }
}
