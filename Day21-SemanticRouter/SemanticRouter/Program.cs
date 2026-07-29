// Day 21: Semantic Routing
// ---------------------------------------------------------------------------
// A high-speed "Router" (SemanticRouter.cs) classifies user intent up front
// via structured JSON, then dispatches to an isolated specialist agent
// (TechSupportAgent.cs / BillingAgent.cs) built with Kernel.Clone() so each
// specialist only has access to its own tools. This scales better than one
// agent holding every tool - fewer options per call, less "tool
// hallucination", and hard tool isolation between domains.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticRouter
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Init the base kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel baseKernel = builder.Build();

            // 2. Init our clean, decoupled components
            var chatService = baseKernel.GetRequiredService<IChatCompletionService>();
            var router = new SemanticRouter(chatService);

            var techAgent = new TechSupportAgent(baseKernel);
            var billingAgent = new BillingAgent(baseKernel);

            Console.WriteLine("Semantic Router Online.  (type 'exit' to quit)");

            while(true)
            {
                Console.Write("\nUser Request: ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input)) continue;
                if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // 3. Step One: Classify the Intent
                RouteDecision decision = await router.RouteAsync(input);
                Console.WriteLine($"\n[ROUTER]: Directed to {decision.Intent} (Reason: {decision.Reasoning})");

                // 4. Step two: Dispatch to the isolated agent
                string response;
                switch(decision.Intent.ToUpper())
                {
                    case "TECH":
                        response = await techAgent.HandleAsync(input);
                        break;

                    case "BILLING":
                        response = await billingAgent.HandleAsync(input);
                        break;

                    default:
                        // General queries don't need tools, just a direct kernel invocation
                        var result = await baseKernel.InvokePromptAsync(input);
                        response = result.ToString();
                        break;
                }

                Console.WriteLine($"[AGENT]: {response}");
            }
        }
    }
}
