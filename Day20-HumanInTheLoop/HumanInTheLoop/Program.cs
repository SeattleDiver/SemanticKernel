using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace HumanInTheLoop
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in HumanInTheLoop.csproj) before building.");
#else
            // 1. Init the kernel
            IKernelBuilder builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY is missing");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY is missing");

            builder.AddGoogleAIGeminiChatCompletion("gemini-3.1-pro-preview", apiKey);
#endif
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

            // The initial prompt sets the first 'User' role in the conversation
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
                    // The human's feedback naturally acts as the 'User' role, keeping the
                    // conversation in a natural User -> Assistant -> User rhythm.
                    history.AddUserMessage($"The draft was rejected.  Please review based on this feedback: {result.Feedback}");
                    Console.WriteLine("\nSending feedback to  the AI...");
                }

                currentRevision++;
            }

            if (!isApproved)
            {
                Console.WriteLine("\n Maximum revisions reached.  Workflow stopped.");
            }
#endif
        }
    }
}