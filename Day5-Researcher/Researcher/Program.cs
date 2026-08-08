// Day 5: The Researcher
// ---------------------------------------------------------------------------
// Answers a question that requires real-time knowledge the model can't have
// from training data alone, using Gemini's built-in Google Search grounding
// tool.
//
// As of Microsoft.SemanticKernel.Connectors.Google 1.77.0-alpha (verified by
// reflecting over the installed DLL), the connector never reads
// PromptExecutionSettings.ExtensionData when it builds a Gemini request - so
// there is currently no supported way to get SK's Gemini connector to send
// the "googleSearch" tool; it is silently dropped and Gemini falls back to
// its training data. We still build the Kernel and GeminiPromptExecutionSettings
// below so this lesson stays consistent with the rest of the series, but the
// actual grounded call is issued directly against Gemini's REST API, since
// that's the only way to guarantee the tools array reaches the model today.
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Day5Researcher
{
    class Program
    {
        private const string ModelId = "gemini-2.5-flash";

        static async Task Main(string[] args)
        {
            // Step 1: Initialize the Kernel with Gemini 2.5 Flash
            var builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? throw new Exception("GEMINI_API_KEY environment variable not set.");

            builder.AddGoogleAIGeminiChatCompletion(ModelId, apiKey);

            // Notice: We do NOT add any Custom Search Plugins to the builder!
            // Kept for parity with the rest of the series; the grounded call
            // below bypasses it, since SK's connector can't carry the tool.
            _ = builder.Build();

            // Step 2: Ask a question that requires real-time knowledge
            string prompt = "What are the top 3 news headlines regarding space exploration today? Summarize each in one sentence.";

            // Step 3: The settings we want Gemini to use for this request.
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,
            };

            Console.WriteLine($"Question: {prompt}\n");
            Console.WriteLine("Agent is thinking and utilizing Gemini's built-in Google Search grounding...\n");

            // Step 4: Execute the grounded request. Wrapped in try/catch
            // because this is the network call to the model - grounding adds
            // an extra live search hop beyond the base chat call, so a bad
            // key, rate limit, or connectivity blip here would otherwise
            // crash the whole program with a raw stack trace instead of a
            // readable message.
            try
            {
                string answer = await AskGeminiWithGoogleSearchAsync(
                    apiKey, ModelId, prompt, executionSettings.Temperature ?? 0.0);

                // Step 5: Display the final result
                Console.WriteLine("--- AI RESEARCH REPORT ---");
                Console.WriteLine(answer.Trim());
                Console.WriteLine("--------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Research request failed: {ex.Message}");
            }
        }

        // Calls Gemini's generateContent REST endpoint directly with the
        // native "googleSearch" grounding tool enabled. This bypasses
        // Semantic Kernel's Gemini connector entirely for this one call,
        // because that connector currently drops the tools array and never
        // sends it to Gemini (see the note at the top of this file).
        private static async Task<string> AskGeminiWithGoogleSearchAsync(
            string apiKey, string modelId, string prompt, double temperature)
        {
            using var httpClient = new HttpClient();

            var requestBody = new JsonObject
            {
                ["contents"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["parts"] = new JsonArray { new JsonObject { ["text"] = prompt } }
                    }
                },
                ["generationConfig"] = new JsonObject { ["temperature"] = temperature },
                // The Gemini API requires camelCase "googleSearch".
                ["tools"] = new JsonArray { new JsonObject { ["googleSearch"] = new JsonObject() } }
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelId}:generateContent?key={apiKey}";

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, requestBody);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Gemini API returned {(int)response.StatusCode}: {responseJson}");
            }

            using JsonDocument document = JsonDocument.Parse(responseJson);
            JsonElement parts = document.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts");

            var textBuilder = new System.Text.StringBuilder();
            foreach (JsonElement part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("text", out JsonElement textElement))
                {
                    textBuilder.Append(textElement.GetString());
                }
            }

            return textBuilder.ToString();
        }
    }
}