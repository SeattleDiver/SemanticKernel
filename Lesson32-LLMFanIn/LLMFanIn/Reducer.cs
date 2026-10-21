using System.Text.Json;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace LLMFanIn
{
    /// <summary>
    /// The fan-in step: combines N independent drafts into a single synthesized answer via an LLM call.
    /// </summary>
    internal class Reducer
    {
        private readonly IChatCompletionService _chatService;

        public Reducer(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Synthesizes the given drafts into one answer that combines their strongest elements.
        /// </summary>
        public async Task<SynthesisResult> SynthesizeAsync(string task, IReadOnlyList<string> drafts)
        {
            string draftsBlock = string.Join(
                "\n\n",
                drafts.Select((draft, index) => $"DRAFT {index + 1}:\n{draft}"));

            string prompt = $$"""
                You are a synthesis editor. {{drafts.Count}} independent writers each attempted
                the task below. Combine their strongest elements into ONE answer that is better
                than any single draft alone - do not just pick one draft verbatim, and do not
                simply concatenate them.

                TASK:
                {{task}}

                {{draftsBlock}}

                Output ONLY valid JSON matching this schema:
                {
                    "synthesizedAnswer": "The single combined answer.",
                    "rationale": "One short sentence per draft noting what, if anything, you kept from it."
                }
                """;

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.2,
                ResponseFormat = typeof(SynthesisResult)
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            try
            {
                return JsonSerializer.Deserialize<SynthesisResult>(response.Content ?? "{}")
                    ?? FallbackTo(drafts);
            }
            catch (JsonException)
            {
                return FallbackTo(drafts);
            }
        }

        /// <summary>
        /// Fallback synthesis used when the reducer's own JSON response can't be parsed.
        /// </summary>
        private static SynthesisResult FallbackTo(IReadOnlyList<string> drafts) => new()
        {
            SynthesizedAnswer = drafts[0],
            Rationale = "Failed to parse synthesis JSON; falling back to the first draft."
        };
    }
}
