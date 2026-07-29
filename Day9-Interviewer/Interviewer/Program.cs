// Day 9: The Interviewer
// ---------------------------------------------------------------------------
// A persona-constrained, multi-turn "mock interviewer" agent. Demonstrates
// shaping model behavior entirely through the system prompt - explicit rules
// (ask one question at a time, probe vague answers, escalate difficulty) -
// layered on top of the same ChatHistory loop introduced in Day 1.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Interviewer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Initialize the Kernel with Gemini 2.5 flash
            var builder = Kernel.CreateBuilder();

            string apikey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ??
                throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            builder.AddGoogleAIGeminiChatCompletion(
                apiKey: apikey,
                modelId: "gemini-2.5-flash");

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
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.7,
            };

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
        }
    }
}