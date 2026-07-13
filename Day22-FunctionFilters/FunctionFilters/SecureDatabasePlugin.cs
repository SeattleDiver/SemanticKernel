using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace FunctionFilters
{
    /// <summary>
    /// A mock data plugin that the AI will autonomously decide to call.
    /// </summary>
    internal class SecureDatabasePlugin
    {
        [KernelFunction("GetCustomerBalance")]
        [Description("Retrieves the account balance for a specific customer ID.")]
        public async Task<string> GetBalanceAsync([Description("The unique customer ID")]string customerId)
        {
            // Simulate a neetwork/database delay
            await Task.Delay(400);

            //string response = $"Customer {customerId} has an active balance of $5,240";
            string response = "$5,240";
            Console.WriteLine($"[DATABASE PLUGIN]: {response}");
            return response;
        }
    }
}
