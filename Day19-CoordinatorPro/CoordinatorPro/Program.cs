// Day 19: The Coordinator (Pro)
// ---------------------------------------------------------------------------
// Progresses from the fixed turn-order of Day 18 to the Coordinator Pattern:
// a meta-agent that reads conversation state and decides, on every
// iteration, which specialist (Coder/Auditor) should speak next - forced to
// respond with strict JSON via ResponseMimeType so routing decisions are
// parsed programmatically instead of guessed from free text. Also pairs
// with RetryHandler.cs, a provider-agnostic resilient HttpClient for
// transient upstream errors.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoordinatorPro
{
    // The strict JSON schema the Coordinator must respond with on every turn.
    public class RoutingDecision
    {
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = "";

        [JsonPropertyName("nextAgent")]
        public string NextAgent { get; set; } = "";
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            var resilientHttpClient = new HttpClient(new RetryHandler(maxRetries: 5));

            // This "meta-agent" episode benefits from a stronger model for its routing
            // reasoning - swap in a Pro-tier Gemini model here if you want to demonstrate
            // that. We've also included a resilient HTTP retry handler to deal with
            // Google's occasional outages (503 Service Unavailable).
            builder.AddGoogleAIGeminiChatCompletion(
                modelId: "gemini-2.5-flash",
                apiKey: apiKey,
                httpClient: resilientHttpClient);

            Kernel kernel = builder.Build();

            IChatCompletionService chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. Define the Agent Personas
            string coderPersona = "You are a C# developer.  Write clean code for the requested feature.  Output only the code.";
            string auditorPersona = "You are a Security Auditor.  Find one flaw in the code.  If it is perfect, say exactly 'LGTM'.";

            string coordinatorInstructions = @"
                You are the project Coordinator.  Analyze the conversation history
                1. If there is no code yet, the nextAgent is the CODER.
                2. If there is code but no security review, the nextAgent is the AUDITOR.
                3. If the Auditor said 'LGTM', the nextAgent is COMPLETE.
                4. If the Auditor requested changes, the nextAgent is CODER.

                You must output your decision in valid JSON matching this schema:
                {
                    ""reasoning"": ""Your step-by-step logic."",
                    ""nextAgent"": ""CODER"" | ""AUDITOR"" | ""COMPLETE""
                }";

            ChatHistory history = new ChatHistory();
            Console.Write("Enter a C# feature to build: ");
            string? goal = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(goal)) return;
            history.AddUserMessage($"Goal: {goal}");

            bool isFinished = false;
            int iteration = 0;
            int maxIterations = 8;

            var coordSettings = new GeminiPromptExecutionSettings()
            {
                ResponseMimeType = "application/json",
                Temperature = 0.0
            };

            while (!isFinished && iteration < maxIterations)
            {
                // ------------------------------------
                // The COORDINATOR decides
                // ------------------------------------

                // The "Ghost Nudge": Add a temporary user message to satisfy Gemini's alternation rule
                var ghostNudge = new ChatMessageContent(AuthorRole.User, "Coordinator, evaluate the state and output the JSON routing decision.");
                history.Add(ghostNudge);

                // Inject Coordinator Persona
                history.Insert(0, new ChatMessageContent(AuthorRole.System, coordinatorInstructions.Trim()));

                var decisionResponse = await chatService.GetChatMessageContentAsync(history, coordSettings);

                // Cleanup history (remove both the system persona and the Ghost nudge
                history.RemoveAt(0);
                history.Remove(ghostNudge);

                // Parse the guaranteed JSON. ResponseMimeType makes malformed JSON rare,
                // but a truncated response or a safety-filter block can still slip
                // through - catch that instead of letting a JsonException crash the app.
                RoutingDecision decision;
                try
                {
                    decision = JsonSerializer.Deserialize<RoutingDecision>(decisionResponse.Content ?? "{}")
                        ?? new RoutingDecision();
                }
                catch (JsonException)
                {
                    Console.WriteLine("\n[ERROR] Coordinator returned malformed JSON. Ending run early.");
                    return;
                }

                Console.WriteLine($"\n[THOUGHT]: {decision.Reasoning}");
                Console.WriteLine($"\n[ROUTE]: {decision.NextAgent}");

                // ------------------------------------
                // ROUTE to the Correct Specialist
                // ------------------------------------

                if (decision.NextAgent == "COMPLETE")
                {
                    isFinished = true;
                }
                else if (decision.NextAgent == "CODER")
                {
                    await CallAgent("Coder", coderPersona, history, chatService);
                }
                else if (decision.NextAgent == "AUDITOR")
                {
                    await CallAgent("Auditor", auditorPersona, history, chatService);
                }

                iteration++;

            }

            Console.WriteLine(isFinished ? "\nProject lifecycle finished." : "\n Max iterations reached.");
        }

        static async Task CallAgent(string name, string persona, ChatHistory history, IChatCompletionService service)
        {
            // Ensure a user message exists betfore this 'Assistant' response
            history.AddUserMessage($"{name}, please perform your task based on the current state.");

            // Persona swapping
            history.Insert(0, new ChatMessageContent(AuthorRole.System, persona));

            var result = await service.GetChatMessageContentAsync(history);

            history.RemoveAt(0);

            string output = result.Content ?? "No output.";
            Console.WriteLine($"\n[{name.ToUpper()}]: {output}");

            history.AddAssistantMessage(output);
        }
    }
}
