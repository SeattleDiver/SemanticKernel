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

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Missing Key");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

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
                var copywriterResult = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);

                // Cleanup the system Persona at index 0
                chatHistory.RemoveAt(0);

                string slogan = copywriterResult.Content ?? "No slogan generated.";
                chatHistory.AddAssistantMessage(slogan);
                Console.WriteLine($"\n[COPYWRITER]: {slogan}");

                // --- ROLE ALTERNATION NUDGE ---
                // This originated as a workaround for Gemini's strict requirement that
                // chat history alternate User -> Assistant -> User. Plain OpenAI chat
                // completions don't enforce that alternation, so this nudge is no longer
                // strictly required here - it's kept because it's harmless and still
                // reads naturally as "handing off" to the next agent.
                chatHistory.AddUserMessage("Editor, please review the slogan above.");

                // ---------------------------
                // Editor turn
                // ---------------------------
                chatHistory.Insert(0, new ChatMessageContent(AuthorRole.System, editorPersona));

                var editorResult = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);

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

            Console.WriteLine(isComplete ? "\nWorkflow Finalized." : "\nMax iterations reached.");
        }
    }
}