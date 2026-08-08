// Day 18: Native Multi-Agent Orchestration
// ---------------------------------------------------------------------------
// Two agents (Copywriter, Editor) collaborating over a single shared
// ChatHistory via manual "persona swapping" - inserting a System message at
// index 0 right before each agent speaks, then removing it once the reply
// comes back. A plain C# while loop drives the whole thing, so the
// hand-off logic between agents stays fully visible instead of being hidden
// inside a framework.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace MultiAgent
{

    /// <summary>Entry point that hand-drives a two-agent Copywriter/Editor collaboration over one shared ChatHistory.</summary>
    class Program
    {
        /// <summary>Runs a bounded persona-swapping loop where a Copywriter drafts a slogan and an Editor reviews it.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // 1. Setup the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Missing Key");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);

            Kernel kernel = builder.Build();

            // 2. Get the chat service
            IChatCompletionService chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 3. Define the Specialist Personas
            string copywriterPersona = "You are a creative copywriter.  Write a punch 5-word slogan for the product provided.";
            string editorPersona = "You are a brand editor.  Review the slogan.  If it's perfect, say 'APPROVED'.  If not, provide one suggestion";

            // 4. Shared conversation history
            ChatHistory chatHistory = new ChatHistory();

            Console.WriteLine("Enter a product to market:");
            string? product = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(product)) return;
            chatHistory.AddUserMessage($"Product: {product}");

            bool isComplete = false;
            bool hadError = false;
            int currentIteration = 0;
            int MaxIterations = 4;

            Console.WriteLine("\n Multi-Agent collaboration started...");

            while (!isComplete && currentIteration < MaxIterations)
            {
                // ---------------------------
                // Copywriter turn
                // ---------------------------

                // Inject the persona at index 0
                chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, copywriterPersona));

                ChatMessageContent copywriterResult;
                try
                {
                    // Step 5: Guard the API call so a transient OpenAI failure reports
                    // cleanly instead of crashing the app mid-collaboration.
                    copywriterResult = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Copywriter call failed: {ex.Message}");
                    chatHistory.RemoveAt(0);
                    hadError = true;
                    break;
                }

                // Cleanup the system Persona at index 0
                chatHistory.RemoveAt(0);

                string slogan = copywriterResult.Content ?? "No slogan generated.";
                chatHistory.AddAssistantMessage(slogan);
                Console.WriteLine($"\n[COPYWRITER]: {slogan}");

                // --- HAND-OFF NUDGE ---
                // OpenAI's API doesn't require strict User/Assistant alternation the
                // way Gemini's did, but this message still earns its place: it's the
                // explicit cue that tells the Editor persona what to do next, since
                // nothing else in shared history says "it's your turn now."
                chatHistory.AddUserMessage("Editor, please review the slogan above.");

                // ---------------------------
                // Editor turn
                // ---------------------------
                chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, editorPersona));

                ChatMessageContent editorResult;
                try
                {
                    // Step 6: Same guard as the Copywriter call - fail loudly but
                    // gracefully instead of taking the whole session down.
                    editorResult = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[ERROR] Editor call failed: {ex.Message}");
                    chatHistory.RemoveAt(0);
                    hadError = true;
                    break;
                }

                chatHistory.RemoveAt(0);

                string review = editorResult.Content ?? "No review generated.";
                chatHistory.AddAssistantMessage(review);
                Console.WriteLine($"\n[EDITOR]: {review}");

                if (review.Contains("APPROVED", StringComparison.OrdinalIgnoreCase))
                {
                    isComplete = true;
                }
                else
                {
                    chatHistory.AddUserMessage("Copywriter, please refine the slogan based on that feedback.");
                }

                currentIteration++;
            }

            if (isComplete)
                Console.WriteLine("\nWorkflow Finalized.");
            else if (hadError)
                Console.WriteLine("\nCollaboration stopped early due to an error.");
            else
                Console.WriteLine("\nMax iterations reached.");
        }
    }
}