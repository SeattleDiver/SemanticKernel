// Day 34: Multi-Agent Group Chat / Debate
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace GroupChatDebate
{
    /// <summary>
    /// Entry point that drives a moderator-selected debate among three agent personas.
    /// </summary>
    internal class Program
    {
        private const int MaxTurns = 6;

        /// <summary>
        /// Runs a bounded number of moderator-selected debate turns and prints a final verdict.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var moderator = new DebateModerator(chatService);
            var speaker = new DebateSpeaker(chatService);

            var participants = new List<DebateParticipant>
            {
                new()
                {
                    Name = "OperationsLead",
                    Description = "Leads customer support operations and worries about coverage.",
                    Instructions =
                        "You are the Operations Lead. You are wary of a 4-day work week because " +
                        "customer response times and coverage matter most to you. Make one focused " +
                        "point per turn, directly engaging with what was just said.",
                },
                new()
                {
                    Name = "EngineeringManager",
                    Description = "Leads the engineering team and worries about burnout and focus time.",
                    Instructions =
                        "You are the Engineering Manager. You favor a 4-day work week because you " +
                        "believe it improves focus and reduces burnout. Make one focused point per " +
                        "turn, directly engaging with what was just said.",
                },
                new()
                {
                    Name = "FinanceAnalyst",
                    Description = "Tracks cost and output metrics and is skeptical of unproven changes.",
                    Instructions =
                        "You are the Finance Analyst. You are skeptical of a 4-day work week until " +
                        "it's proven not to hurt output-per-dollar. Make one focused point per turn, " +
                        "directly engaging with what was just said.",
                },
            };

            const string topic = "Should the company adopt a 4-day work week? Debate it.";
            Console.WriteLine($"TOPIC: {topic}\n");

            var transcriptLines = new List<string>();
            var speakerHistory = new List<string>();

            for (int turn = 1; turn <= MaxTurns; turn++)
            {
                string transcriptSoFar = string.Join("\n\n", transcriptLines);
                string? lastSpeaker = speakerHistory.Count > 0 ? speakerHistory[^1] : null;
                var spokenInLastTwoTurns = speakerHistory.TakeLast(2).ToHashSet();

                SpeakerDecision decision = await moderator.SelectNextSpeakerAsync(
                    participants, transcriptSoFar, lastSpeaker, spokenInLastTwoTurns);
                DebateParticipant nextSpeaker = participants.First(p => p.Name == decision.NextSpeaker);

                string response = await speaker.RespondAsync(nextSpeaker, transcriptSoFar);

                Console.WriteLine($"--- {nextSpeaker.Name} (moderator: {decision.Reason}) ---\n{response}\n");

                transcriptLines.Add($"{nextSpeaker.Name}: {response}");
                speakerHistory.Add(nextSpeaker.Name);
            }

            string verdict = await moderator.SummarizeAsync(string.Join("\n\n", transcriptLines));

            Console.WriteLine("--- FINAL VERDICT ---");
            Console.WriteLine(verdict);
        }
    }
}
