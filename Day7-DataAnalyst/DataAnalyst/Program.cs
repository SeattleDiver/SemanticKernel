// Day 7: The Data Analyst
// ---------------------------------------------------------------------------
// An extraction agent: instead of asking the model for a conversational
// reply, it forces every response into a fixed JSON shape via Gemini's
// ResponseMimeType/ResponseSchema, then deserializes that JSON straight into
// a typed FeedbackAnalysis object with System.Text.Json. Doing this across a
// small batch of messy, free-text customer feedback turns the model's output
// into something a normal C# program can aggregate and report on - the thing
// free-text output can't reliably do.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace DataAnalyst
{
    // The fixed schema every extraction must match. JsonPropertyName keeps the
    // wire format lowerCamelCase (what we ask the model for in the prompt)
    // while the C# side stays PascalCase.
    public class FeedbackAnalysis
    {
        [JsonPropertyName("sentiment")]
        public string Sentiment { get; set; } = "Unknown";

        [JsonPropertyName("product")]
        public string Product { get; set; } = "Unknown";

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = "";

        [JsonPropertyName("priority")]
        public int Priority { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 1: Initialize the Kernel
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
            Kernel kernel = builder.Build();

            // Step 2: Force every call to return JSON matching FeedbackAnalysis.
            // ResponseSchema constrains the field names/types the model can
            // return; ResponseMimeType is what actually turns JSON mode on.
            var executionSettings = new GeminiPromptExecutionSettings
            {
                ResponseMimeType = "application/json",
                ResponseSchema = typeof(FeedbackAnalysis),
                Temperature = 0.0
            };

            // Step 3: A small batch of raw, unstructured customer feedback -
            // the kind of free text a support inbox actually receives.
            string[] rawFeedback = new[]
            {
                "The new dashboard is great, loads way faster than before! Nice job on the redesign.",
                "I've been trying to export my report to CSV for 20 minutes and it just spins forever. This is blocking my whole team.",
                "Not sure how I feel about the new pricing page, it's a little confusing but I guess it's fine.",
                "Your app crashed and I lost an hour of unsaved work on the Invoicing screen. This needs to be fixed immediately."
            };

            string promptTemplate = @"
You are a data analyst extracting structured information from raw customer feedback.
Read the feedback below and extract exactly these fields:
- sentiment: one of ""Positive"", ""Neutral"", ""Negative""
- product: the product or feature area being discussed (use ""Unknown"" if unclear)
- summary: a one-sentence, neutral summary of the feedback
- priority: an integer from 1 (no action needed) to 5 (urgent, needs immediate attention)

Respond with only the JSON object. Do not include any commentary.

FEEDBACK:
{{$feedback}}
";

            Console.WriteLine($"Analyzing {rawFeedback.Length} feedback entries...\n");

            var analyses = new List<FeedbackAnalysis>();

            // Step 4: Extract each entry independently and deserialize the result.
            foreach (string feedback in rawFeedback)
            {
                var arguments = new KernelArguments(executionSettings)
                {
                    { "feedback", feedback }
                };

                FunctionResult result;
                try
                {
                    result = await kernel.InvokePromptAsync(promptTemplate, arguments);
                }
                catch (Exception ex)
                {
                    // A transient network error or rate limit here would otherwise
                    // crash the whole batch over a single bad entry - skip it and
                    // keep processing the rest instead.
                    Console.WriteLine($"[ERROR] Could not analyze entry: {ex.Message}");
                    continue;
                }

                // Step 5: Deserialize the guaranteed-JSON response into our typed
                // object. ResponseSchema makes malformed JSON rare, but a
                // safety-filter block or truncated response can still slip
                // through, so this is guarded the same way Day 19's Coordinator
                // guards its own JSON parsing.
                try
                {
                    var analysis = JsonSerializer.Deserialize<FeedbackAnalysis>(result.ToString())
                        ?? new FeedbackAnalysis();
                    analyses.Add(analysis);

                    Console.WriteLine($"[{analysis.Sentiment,-8}] P{analysis.Priority} - {analysis.Product}: {analysis.Summary}");
                }
                catch (JsonException)
                {
                    Console.WriteLine("[ERROR] Model returned malformed JSON for one entry - skipping it.");
                }
            }

            // Step 6: Aggregate the typed results into a report. This is the
            // payoff of structured output: counting and filtering by field
            // is now a LINQ query, not a second pass of prompting the model.
            Console.WriteLine("\n--- ANALYST REPORT ---");
            Console.WriteLine($"Total analyzed: {analyses.Count}");
            foreach (var group in analyses.GroupBy(a => a.Sentiment))
            {
                Console.WriteLine($"  {group.Key}: {group.Count()}");
            }

            var urgent = analyses.Where(a => a.Priority >= 4).ToList();
            Console.WriteLine($"Urgent items (priority >= 4): {urgent.Count}");
            foreach (var item in urgent)
            {
                Console.WriteLine($"  - [{item.Product}] {item.Summary}");
            }
            Console.WriteLine("----------------------");
        }
    }
}
