using Microsoft.SemanticKernel;

namespace UniversalProjectManager
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Initialize the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            Console.WriteLine("Universal Project Manager (UPM) Initialized");
            Console.WriteLine("Architecture: Blackboard Pattern + Interface-Driven Agents");

            // Setup the Shared State
            ProjectState projectState = new ProjectState();
            
            Console.Write("Enter a rough project idea:");
            string originalRequest = Console.ReadLine() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(originalRequest)) return;

            projectState.OriginalRequest = originalRequest;
            
            // Initialize and Execute the Preliminary Agent
            IProjectAgent refiner = new GoalRefinerAgent(kernel);

            Console.WriteLine($"\n[{refiner.Name.ToUpper()}] is processing the request..."); 
            await refiner.ExecuteAsync(projectState);

            // Output the resulting Architecture state
            Console.WriteLine("\n--- CURRENT PROJECT STATE ---");
            Console.WriteLine($"Original : {projectState.OriginalRequest}");
            Console.WriteLine($"Refined  : {projectState.RefinedGoal}");
            Console.WriteLine($"Tasks    : {projectState.Tasks.Count} (Pending Generation)");
            Console.WriteLine($"Complete : {projectState.IsFullyCompleted}");
            Console.WriteLine("-----------------------------");
            Console.WriteLine("\nArchitecture validated. Ready for Planners and Developers in Day 28.");
        }
    }
}
