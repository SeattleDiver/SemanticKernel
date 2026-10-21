using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace GroupChatDebate
{
    /// <summary>
    /// Chooses who speaks next in a debate and, once it ends, summarizes the outcome.
    /// </summary>
    internal class DebateModerator
    {
        private readonly IChatCompletionService _chatService;

        public DebateModerator(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Picks the next speaker from the given candidates, excluding whoever just spoke and
        /// forcing in anyone who has been quiet for the last two turns.
        /// </summary>
        public async Task<SpeakerDecision> SelectNextSpeakerAsync(
            IReadOnlyList<DebateParticipant> participants, string transcript, string? lastSpeaker,
            IReadOnlySet<string> spokenInLastTwoTurns)
        {
            // The most recent speaker is removed from the candidate pool entirely, not just
            // discouraged by prompt wording - see Program.cs for why that matters.
            var candidates = participants.Where(p => p.Name != lastSpeaker).ToList();
            if (candidates.Count == 0)
            {
                candidates = participants.ToList();
            }

            string roster = string.Join("\n", candidates.Select(p => $"- {p.Name}: {p.Description}"));
            string transcriptText = transcript.Length == 0 ? "(the debate has not started yet)" : transcript;

            var overdue = candidates.Where(p => !spokenInLastTwoTurns.Contains(p.Name)).ToList();
            string overdueNote = overdue.Count > 0
                ? $"{string.Join(", ", overdue.Select(p => p.Name))} {(overdue.Count == 1 ? "hasn't" : "haven't")} spoken in a while - you MUST pick one of them now instead of continuing a two-way back-and-forth, even if someone else was just challenged."
                : "Everyone has spoken recently - pick based on who was just challenged.";

            string prompt = $$"""
                You are moderating a debate between 3+ participants. Decide who should speak next.
                A two-person debate isn't the goal here - a good moderator keeps rotating in every
                voice, not just letting two people volley back and forth. The most recent speaker
                is excluded below since they already just had a turn.

                {{overdueNote}}

                ELIGIBLE PARTICIPANTS:
                {{roster}}

                TRANSCRIPT SO FAR:
                {{transcriptText}}

                Output ONLY valid JSON matching this schema:
                {
                    "nextSpeaker": "the exact name of the participant who should speak next",
                    "reason": "one short sentence explaining the choice"
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.3,
                ResponseFormat = "json_object"
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                var decision = JsonSerializer.Deserialize<SpeakerDecision>(response.Content ?? "{}");
                if (decision is not null && candidates.Any(p => p.Name == decision.NextSpeaker))
                {
                    return decision;
                }
            }
            catch (JsonException)
            {
                // fall through to the fallback below
            }

            return new SpeakerDecision
            {
                NextSpeaker = candidates[0].Name,
                Reason = "Fallback: moderator response was unparseable."
            };
        }

        /// <summary>
        /// Produces a final verdict summarizing the strongest point each side made.
        /// </summary>
        public async Task<string> SummarizeAsync(string transcript)
        {
            string prompt = $$"""
                The debate below has ended. Summarize the strongest point each side made and
                give a final verdict on the original question in 3-4 sentences total.

                TRANSCRIPT:
                {{transcript}}
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.3 };
            var response = await _chatService.GetChatMessageContentAsync(history, settings);
            return response.Content ?? "(no summary produced)";
        }
    }
}
