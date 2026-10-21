using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Text.Json;

namespace Evaluator
{
    /// <summary>The LLM-as-a-judge: scores a target agent's response against a known ground truth using a strict rubric.</summary>
    internal class JudgeAgent
    {
        private readonly IChatCompletionService _chatService;

        /// <summary>Creates a judge that scores responses using the given chat service.</summary>
        /// <param name="chatService">The chat completion service used to generate judgments.</param>
        public JudgeAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>Scores a target agent's response against the ground truth for a given question.</summary>
        /// <param name="question">The original question asked.</param>
        /// <param name="targetResponse">The target agent's response to evaluate.</param>
        /// <param name="groundTruth">The known-correct answer to compare against.</param>
        /// <returns>The judge's <see cref="EvaluationResult"/>, or a failure result if its JSON couldn't be parsed.</returns>
        public async Task<EvaluationResult> EvaluateAsync(string question, string targetResponse, string groundTruth)
        {
            // Build the Rubric
            string prompt = $@"
                You are a strict Quality Assurance Judge.
                Evaluate the 'Agent Response' based on its accuracy compared to the 'Ground Truth'.

                RUBRIC:
                - Score 5: Perfectly accurate and aligns with the ground truth.
                - Score 3: Partially accurate, but missing key details.
                - Score 1: Inaccurate or contradicts the ground truth.
                - PASSED: True if score is 4 or 5.  False otherwise.

                User Question: {question}
                Ground Truth: {groundTruth}
                Agent Response: {targetResponse}

                Output ONLY valid JSON matching this schema:
                {{
                    ""reasoning"": ""Your step-by-step logic"",
                    ""score"": [1-5],
                    ""passed"": true/false
                }}
            ";

            var history = new ChatHistory();
            history.AddUserMessage(prompt);

            // Strict Execution Settings for Evaluation
            var settings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0,
                ResponseFormat = "json_object"
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            // Deserialize the results. The judge can occasionally return
            // malformed or truncated JSON even with ResponseFormat set,
            // so we catch the parse failure here and fail this test case
            // gracefully instead of letting an unhandled exception abort
            // the whole evaluation suite.
            try
            {
                return JsonSerializer.Deserialize<EvaluationResult>(response.Content ?? "{}")
                    ?? new EvaluationResult { Passed = false, Reasoning = "Failed to parse JSON" };
            }
            catch (JsonException)
            {
                return new EvaluationResult { Passed = false, Reasoning = "Failed to parse JSON" };
            }
        }
    }
}
