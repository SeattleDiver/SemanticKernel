// Day 20: Human-in-the-Loop
// ---------------------------------------------------------------------------
// A production-critical pattern: certain AI-generated actions (here, a
// corporate email announcement) should never ship fully autonomously. This
// wires an AI drafting agent (AiWorker.cs) to a hard human approval gate
// (HumanGatekeeper.cs) that the model cannot talk its way past - only an
// explicit "APPROVED" from a human ends the loop.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HumanInTheLoop
{
    /// <summary>Entry point that pairs an AI drafting worker with a hard human-approval gate the model cannot talk past.</summary>
    class Program
    {
        /// <summary>Runs a bounded draft/review loop that only ends on an explicit human "APPROVED" verdict.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // 1. Init the kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. Initialize our clean, signle-responsibility objects
            string persona = "You are a Corporate Communications Directory.  Write professional, concise email announcements.";
            var aiWorker = new AiWorker(chatService, persona);
            var gatekeeper = new HumanGatekeeper();

            ChatHistory history = new ChatHistory();

            Console.WriteLine("Corporate Comms System Initialized.");
            Console.Write("What is the top of the announcement? ");
            string? topic = Console.ReadLine();

            // The initial prompt sets the first 'User' turn in the conversation
            history.AddUserMessage($"Draft an email announcement regarding: {topic}");

            bool isApproved = false;
            bool aiServiceFailed = false;
            int maxRevisions = 5;
            int currentRevision = 0;

            // 3. The Orchestration Loop
            while(!isApproved && currentRevision < maxRevisions)
            {
                Console.WriteLine("\n[AI IS DRAFTING...]");

                string draft;
                try
                {
                    draft = await aiWorker.GenerateDraftAsync(history);
                }
                catch (Exception ex)
                {
                    // Step: A transient network/API failure should not crash the
                    // whole approval workflow with an unhandled exception - report
                    // it and stop the loop cleanly instead.
                    Console.WriteLine($"\n[ERROR] The AI service call failed: {ex.Message}");
                    Console.WriteLine("Workflow stopped due to an AI service error.");
                    aiServiceFailed = true;
                    break;
                }

                // 4. Yield control to the human
                ReviewResult result = gatekeeper.ReviewDraft(draft);

                if (result.IsApproved)
                {
                    isApproved = true;
                    Console.WriteLine("\nThe announcement has been approved and is ready to send.");
                }
                else
                {
                    // The human's feedback naturally acts as the next 'User' turn,
                    // keeping the conversation as a clean user/assistant back-and-forth.
                    history.AddUserMessage($"The draft was rejected.  Please review based on this feedback: {result.Feedback}");
                    Console.WriteLine("\nSending feedback to the AI...");
                }

                currentRevision++;
            }

            if (!isApproved && !aiServiceFailed)
            {
                Console.WriteLine("\nMaximum revisions reached.  Workflow stopped.");
            }

        }
    }
}