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
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Interviewer
{
    /// <summary>Entry point that runs a persona-constrained, multi-turn mock technical interview loop.</summary>
    class Program
    {
        /// <summary>Primes the interviewer persona, then loops on candidate answers until the candidate types "exit".</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // 1. Initialize the Kernel with an OpenAI chat model
            var builder = Kernel.CreateBuilder();

            string apikey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ??
                throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            builder.AddOpenAIChatCompletion(
                apiKey: apikey,
                modelId: "gpt-4.1-mini");

            Kernel kernel = builder.Build();

            // 2. Get the Chat Completion Service
            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 3. Define the System Persona with clear constraints
            var chatHistory = new ChatHistory(
                "You are a senior .NET architect conducting a technical interview for a c# developer position. " +
                "Your goal is to assess the candidate's understanding of Agentic AI and Semantic Kernel. " +
                "Rules: " +
                "1. Ask only ONE question at a time. " +
                "2. If the user's answer is vague, ask a follow-up question to probe deeper. " +
                "3. If the answer is correct, acknowledge it briefly and move to a more difficult topic. " +
                "4. Stay professional and stay in character. " +
                "5. Start by introducing yourself and asking the first question."
            );
            chatHistory.AddUserMessage("I am here for the .NET Architect interview. I'm ready to begin.");

            Console.WriteLine("--- Interview Mode started ---");

            // Consistent execution settings for every model call in this conversation
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.7,
            };

            // 4. Initial propt to trigger the first question
            ChatMessageContent response;
            try
            {
                response = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel: kernel);
            }
            catch (Exception ex)
            {
                // Step: An unguarded API failure here would crash the app before the interview even starts.
                Console.WriteLine($"The interviewer is unavailable right now: {ex.Message}");
                return;
            }

            // Step: Guard against a null/empty first response the same way the loop below already does,
            // since AddAssistantMessage requires a non-null string.
            if (response.Content != null)
            {
                Console.WriteLine($"Interviewer: {response.Content}");
                chatHistory.AddAssistantMessage(response.Content);
            }

            // 5. The interview loop
            while (true)
            {
                Console.Write("\nCandidate: ");
                string? candidateAnswer = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(candidateAnswer)) continue;
                if (candidateAnswer.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // Add the user's answer to the context
                chatHistory.AddUserMessage(candidateAnswer);

                // Get the next response based on the full history
                ChatMessageContent nextQuestion;
                try
                {
                    nextQuestion = await chatService.GetChatMessageContentAsync(chatHistory, executionSettings, kernel: kernel);
                }
                catch (Exception ex)
                {
                    // Step: A transient API failure shouldn't end the interview -
                    // roll back the candidate's unanswered turn so history stays a
                    // clean back-and-forth and the candidate can genuinely retry.
                    chatHistory.RemoveAt(chatHistory.Count - 1);
                    Console.WriteLine($"\nThe interviewer is momentarily unavailable ({ex.Message}). Please try again.");
                    continue;
                }

                // Guard against a null/empty response the same way the priming call above does,
                // since AddAssistantMessage requires a non-null string.
                if (nextQuestion.Content != null)
                {
                    Console.WriteLine($"\nInterviewer: {nextQuestion.Content}");
                    chatHistory.AddAssistantMessage(nextQuestion.Content);
                }
            }
        }
    }
}