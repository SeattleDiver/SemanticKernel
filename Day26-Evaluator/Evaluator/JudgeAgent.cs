using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;
using System.Text.Json;

namespace Evaluator
{
    internal class JudgeAgent
    {
        private readonly IChatCompletionService _chatService;

        public JudgeAgent(IChatCompletionService chatService)
        {
            _chatService = chatService;
        }

        public async Task<EvaluationResult> EvaluateAsync(string question, string targetResponse, string groundTruth)
        {
            // Build the Rubric
            string prompt = $@"
                You are a string Quality Assurance Judge.
                Evaluate the 'Agent Response' based on its accuracy compared to the 'Ground Truth'.

                RUBRIC:
                - Score 5: Perfectly accurate and aligns with the ground truth.
                - Score 3: Partially accurate, but missing key deatils.
                - Score 1: Inaccurate or contradicts the ground trhuth.
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

            // Strict Execution Setings for Evaluation
            var settings = new GeminiPromptExecutionSettings
            {

                Temperature = 0.0,
                ResponseMimeType = "application/json"
            };

            var response = await _chatService.GetChatMessageContentAsync(history, settings);

            // Deserialize the results. The judge can occasionally return
            // malformed or truncated JSON even with ResponseMimeType set,
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
