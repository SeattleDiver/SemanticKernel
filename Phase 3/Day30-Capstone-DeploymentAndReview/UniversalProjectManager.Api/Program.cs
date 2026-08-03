
using Microsoft.SemanticKernel;

namespace UniversalProjectManager.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();

            // Configure the SemanticKernel for dependency injection
            builder.Services.AddTransient<Kernel>(sp =>
            {
                string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                    ?? throw new Exception("GEMINI_API_KEY is missing");

                IKernelBuilder kernelBuilder = Kernel.CreateBuilder();
                kernelBuilder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

                return kernelBuilder.Build();
            });

            // 3. Register Agents as Transient services
            builder.Services.AddTransient<IProjectAgent, GoalRefinerAgent>();
            builder.Services.AddTransient<IProjectAgent, PlannerAgent>();
            builder.Services.AddTransient<IProjectAgent, DeveloperAgent>();
            builder.Services.AddTransient<IProjectAgent, ReviewerAgent>();

            // 4. Register the Orchestrator
            builder.Services.AddTransient<ProjectOrchestrator>();
            var app = builder.Build();

            // Map controller routes
            app.MapControllers();

            Console.WriteLine("Universal Project Manager API is running...");
            app.Run();
        }
    }
}
