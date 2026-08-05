// Day 18: Native Multi-Agent Orchestration
// ---------------------------------------------------------------------------
// Two agents (Copywriter, Editor) collaborating over a single shared
// ChatHistory via manual "persona swapping" - inserting a System message at
// index 0 right before each agent speaks, then removing it once the reply
// comes back. A plain C# while loop drives the whole thing, so the
// hand-off logic between agents stays fully visible instead of being hidden
// inside a framework.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Day18NativeOrchestration
{

    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Setup the Kernel
            IKernelBuilder builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("Missing Key");
            
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);

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
                    // Step 5: Guard the API call so a transient Gemini failure reports
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

                // --- GEMINI PROTOCOL NUDGE ---
                // Gemini requires User -> Assistant alternating conversation
                // We add a 'User' instruction to prepare for the Editor's turn
                chatHistory.AddUserMessage("Editor, please review the slogan above.");

                // ---------------------------
                // Editor turn
                // ---------------------------
                chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, editorPersona));

                ChatMessageContent editorResult;
                try
                {
                    // Step 5: Same guard as the Copywriter call - fail loudly but
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