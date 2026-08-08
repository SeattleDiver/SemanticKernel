// Day 19: The Coordinator (Pro)
// ---------------------------------------------------------------------------
// Progresses from the fixed turn-order of Day 18 to the Coordinator Pattern:
// a meta-agent that reads conversation state and decides, on every
// iteration, which specialist (Coder/Auditor) should speak next - forced to
// respond with strict JSON via ResponseFormat so routing decisions are
// parsed programmatically instead of guessed from free text. Also pairs
// with RetryHandler.cs, a provider-agnostic resilient HttpClient for
// transient upstream errors.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoordinatorPro
{
    // The strict JSON schema the Coordinator must respond with on every turn.
    /// <summary>The Coordinator's strict-JSON routing decision: its reasoning and which agent should act next.</summary>
    public class RoutingDecision
    {
        /// <summary>The Coordinator's step-by-step reasoning for this routing decision.</summary>
        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = "";

        /// <summary>Which agent should act next: "CODER", "AUDITOR", or "COMPLETE".</summary>
        [JsonPropertyName("nextAgent")]
        public string NextAgent { get; set; } = "";
    }

    /// <summary>Entry point that routes a Coder/Auditor pair through a model-driven Coordinator instead of a fixed turn order.</summary>
    internal class Program
    {
        /// <summary>Runs a bounded loop where a Coordinator agent decides which specialist acts next based on conversation state.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            var resilientHttpClient = new HttpClient(new RetryHandler(maxRetries: 5));

            // This "meta-agent" episode benefits from a stronger model for its routing
            // reasoning - swap in a higher-tier OpenAI model here if you want to
            // demonstrate that. We've also included a resilient HTTP retry handler to
            // deal with the provider's occasional outages (503 Service Unavailable).
            builder.AddOpenAIChatCompletion(
                modelId: "gpt-4.1-mini",
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

            var coordSettings = new OpenAIPromptExecutionSettings()
            {
                ResponseFormat = "json_object",
                Temperature = 0.0
            };

            while (!isFinished && iteration < maxIterations)
            {
                // ------------------------------------
                // The COORDINATOR decides
                // ------------------------------------

                // The "Ghost Nudge": a temporary user message that tells the Coordinator
                // persona it's their turn to act. OpenAI doesn't require strict
                // User/Assistant alternation the way Gemini did, but this still earns
                // its place as an explicit "evaluate now" cue.
                var ghostNudge = new ChatMessageContent(AuthorRole.User, "Coordinator, evaluate the state and output the JSON routing decision.");
                history.Add(ghostNudge);

                // Inject Coordinator Persona
                history.Insert(0, new ChatMessageContent(AuthorRole.System, coordinatorInstructions.Trim()));

                var decisionResponse = await chatService.GetChatMessageContentAsync(history, coordSettings);

                // Cleanup history (remove both the system persona and the Ghost nudge
                history.RemoveAt(0);
                history.Remove(ghostNudge);

                // Parse the guaranteed JSON. ResponseFormat makes malformed JSON rare,
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

        /// <summary>Swaps in a specialist's persona, invokes it against shared history, prints its output, and appends the reply.</summary>
        /// <param name="name">The specialist's display name, used in console output.</param>
        /// <param name="persona">The system-prompt persona to swap in for this call.</param>
        /// <param name="history">The shared conversation history both specialists and the Coordinator read from.</param>
        /// <param name="service">The chat completion service to invoke.</param>
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
