// Day 5: The Researcher
// ---------------------------------------------------------------------------
// Answers a question that requires real-time knowledge the model can't have
// from training data alone, using OpenAI's built-in web search grounding.
//
// BREAKING CHANGE vs. the original Gemini version of this lesson: Gemini
// exposed grounding as a "googleSearch" tool flag on its general-purpose
// chat model. OpenAI does not have an equivalent flag for general-purpose
// Chat Completions models: the "web_search" tool that looks like a direct
// analog only exists on the newer Responses API, and as of mid-2026 it is
// documented to intermittently return HTTP 500 server errors specifically
// when paired with gpt-4.1-mini (see OpenAI's developer community bug
// reports) - not something to build a teaching example on.
//
// The current best-supported alternative is OpenAI's purpose-built search
// model, "gpt-4o-mini-search-preview". Grounding is baked into the model
// itself rather than toggled by a tool flag: passing an empty
// "web_search_options" object on a normal Chat Completions request is
// enough to turn on live web search. Semantic Kernel's OpenAI connector has
// no strongly-typed wrapper for "web_search_options" today, so - exactly as
// the original Gemini lesson did with its own connector's gap - we still
// build the Kernel below for series consistency, but issue the actual
// grounded call directly against OpenAI's REST API.
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.SemanticKernel;

namespace Day5Researcher
{
    /// <summary>Entry point that answers a real-time question using OpenAI's search-preview model instead of the standard chat model.</summary>
    class Program
    {
        /// <summary>The series-standard chat model, used only to build the Kernel for consistency with the rest of the series.</summary>
        private const string ChatModelId = "gpt-4.1-mini";

        /// <summary>OpenAI's dedicated web-search model - grounding is a property of this model, not a toggle on the standard chat model.</summary>
        private const string SearchModelId = "gpt-4o-mini-search-preview";

        /// <summary>Builds the (unused) series-standard Kernel, then asks the search-preview model a question requiring live web results.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 1: Initialize the Kernel with the series-standard chat model
            var builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                            ?? throw new Exception("OPENAI_API_KEY environment variable not set.");

            builder.AddOpenAIChatCompletion(ChatModelId, apiKey);

            // Notice: We do NOT add any Custom Search Plugins to the builder!
            // Kept for parity with the rest of the series; the grounded call
            // below bypasses it, since grounding here is a model choice, not
            // something SK's connector or a plugin can turn on for us.
            _ = builder.Build();

            // Step 2: Ask a question that requires real-time knowledge
            string prompt = "What are the top 3 news headlines regarding space exploration today? Summarize each in one sentence.";

            Console.WriteLine($"Question: {prompt}\n");
            Console.WriteLine("Agent is thinking and utilizing OpenAI's built-in web search grounding...\n");

            // Step 3: Execute the grounded request. Wrapped in try/catch
            // because this is the network call to the model - grounding adds
            // an extra live search hop beyond the base chat call, so a bad
            // key, rate limit, or connectivity blip here would otherwise
            // crash the whole program with a raw stack trace instead of a
            // readable message.
            try
            {
                string answer = await AskOpenAIWithWebSearchAsync(apiKey, SearchModelId, prompt);

                // Step 4: Display the final result
                Console.WriteLine("--- AI RESEARCH REPORT ---");
                Console.WriteLine(answer.Trim());
                Console.WriteLine("--------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Research request failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Calls OpenAI's chat completions REST endpoint directly against the search-preview model with web
        /// search enabled. This bypasses Semantic Kernel's OpenAI connector entirely for this one call, because
        /// that connector has no strongly-typed property for the "web_search_options" request field (see the
        /// note at the top of this file). Note that search-preview models reject sampling parameters like
        /// "temperature" and "top_p" outright, so this request intentionally omits them.
        /// </summary>
        /// <param name="apiKey">OpenAI API key used to authenticate the REST call.</param>
        /// <param name="modelId">The search-preview model to call.</param>
        /// <param name="prompt">The user's question requiring live web results.</param>
        /// <returns>The model's response text, including any inline web citations it chose to add.</returns>
        private static async Task<string> AskOpenAIWithWebSearchAsync(string apiKey, string modelId, string prompt)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new JsonObject
            {
                ["model"] = modelId,
                ["messages"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["role"] = "user",
                        ["content"] = prompt
                    }
                },
                // An empty object is enough to turn on grounding for a search-preview model.
                ["web_search_options"] = new JsonObject()
            };

            const string url = "https://api.openai.com/v1/chat/completions";

            using HttpResponseMessage response = await httpClient.PostAsJsonAsync(url, requestBody);
            string responseJson = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"OpenAI API returned {(int)response.StatusCode}: {responseJson}");
            }

            using JsonDocument document = JsonDocument.Parse(responseJson);
            string? content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }
    }
}