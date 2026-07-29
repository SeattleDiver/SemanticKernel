using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace Interviewer
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in Interviewer.csproj) before building.");
#else
            // 1. Initialize the Kernel with a chat model
            var builder = Kernel.CreateBuilder();

#if CHATGPT
            string apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(
                apiKey: apikey,
                modelId: modelId);
#elif GOOGLE
            string apikey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
                throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(
                apiKey: apikey,
                modelId: modelId);
#endif

            Kernel kernel = builder.Build();

            // 2. Get the Chat Completion Service
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 3. Define the System Persona with clear constraints
            var chatHistory = new ChatHistory(
                "You are a senior .NET architect conducting a techincal interview for a c# developer position." +
                "Your goal is to assess the candidate's understanding of Agentic AI and Semantic Kernel. " +
                "Rules" +
                "1. Ask only ONE question at a time" +
                "2. If the user's answer is vague, ask a follow-up qeustion to probe deeper." +
                "3. If the answer is correct, acknowledge it briefly and move to a more difficult topic. " +
                "4. Stay professional and stay in character. " + 
                "5. Start by introducing yourself and asking the first question."
            );
            chatHistory.AddUserMessage("I am here for the .NET Architect interview. I'm ready to begin.");

            Console.WriteLine("--- Interview Mode started ---");

            // Consistent execution settings for every model call in this conversation
#if CHATGPT
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
            };
#elif GOOGLE
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.7,
            };
#endif

            // 4. Initial propt to trigger the first question
            var response = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel: kernel);
            Console.WriteLine($"Interviewer: {response.Content}");
            chatHistory.AddAssistantMessage(response.Content);

            // 5. The interview loop
            while (true)
            {
                Console.Write("\nCandidate: ");
                string? candidateAnswer = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(candidateAnswer)) continue;
                if (candidateAnswer .Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // Add the user's answer to the context
                chatHistory.AddUserMessage(candidateAnswer);

                // Get the next response based on the full history
                var nextQuestion = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel: kernel);
                Console.WriteLine($"\nInterviewer: {nextQuestion.Content}");

                // Add the interviewer's response to the history
                if (nextQuestion.Content != null)
                {
                    chatHistory.AddAssistantMessage(nextQuestion.Content);
                }
            }
#endif
        }
    }
}