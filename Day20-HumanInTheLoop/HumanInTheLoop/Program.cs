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
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Init the kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
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

            // The initial prompt sets the first 'User' role for Gemini
            history.AddUserMessage($"Draft an email announcement regarding: {topic}");

            bool isApproved = false;
            int maxRevisions = 5;
            int currentRevision = 0;

            // 3. The Orchestration Loop
            while(!isApproved && currentRevision < maxRevisions)
            {
                Console.WriteLine("\n[AI IS DRAFTING...]");
                string draft = await aiWorker.GenerateDraftAsync(history);

                // 4. Yield control to the human
                ReviewResult result = gatekeeper.ReviewDraft(draft);

                if (result.IsApproved)
                {
                    isApproved = true;
                    Console.WriteLine("\nThe announcement has been approved and is ready to send.");
                }
                else
                {
                    // GEMINI PROTOCOL: The human's feedback naturally acts as the 'User' role,
                    // perfectly satisfying Gemini's User -> Assistant -> User requirement.
                    history.AddUserMessage($"The draft was rejected.  Please review based on this feedback: {result.Feedback}");
                    Console.WriteLine("\nSending feedback to  the AI...");
                }

                currentRevision++;
            }

            if (!isApproved)
            {
                Console.WriteLine("\n Maximum revisions reached.  Workflow stopped.");
            }

        }
    }
}