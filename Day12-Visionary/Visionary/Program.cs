// Day 12: The Visionary
// ---------------------------------------------------------------------------
// Multimodal input: builds a ChatMessageContentItemCollection combining a
// text instruction with image bytes (TextContent + ImageContent) so the
// model can analyze a chart image instead of just reading text. These
// content types are plain Semantic Kernel abstractions, not Gemini-specific -
// only the connector registration below is provider-specific.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Visionary
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Initialize the Kernel 
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();

            // 2. Define the visionary persona
            var chatHistory = new ChatHistory("You are a specialized Visual Agent. " +
                "Your goal is to describe images with extreme detail and extract any text or data points visible.");

            // 3. Prepare the Multimodal Message
            // We bundle a local sample chart image with the project rather than fetching one
            // from a third-party URL, so this demo doesn't silently break if that page ever
            // moves or the image is taken down.
            string imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-chart.png");

            // Step 3b: Fail fast with a clear message if the asset wasn't copied to the
            // output directory (e.g. bin/obj was cleaned without a rebuild), instead of
            // letting a raw FileNotFoundException surface deep inside the file read.
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Could not find the sample image at: {imagePath}");
                Console.WriteLine("Rebuild the project so the Assets\\sample-chart.png " +
                    "CopyToOutputDirectory step in Visionary.csproj runs again.");
                return;
            }

            Console.WriteLine($"Analyzing Image {imagePath}");

            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);

            var messageItems = new ChatMessageContentItemCollection
            {
                new TextContent("Analyze this image.  Identify the type of chart, the key data points, and summarize the main trend."),
                new ImageContent(imageBytes, "image/png")
            };

            chatHistory.AddUserMessage(messageItems);

            // 4. Invoke the model
            // Multimodal tasks often benefit from a slightly higher temperature to encourage creative descriptions
            var settings = new GeminiPromptExecutionSettings { Temperature = 0.4 };

            // Step 4b: Not every model/region accepts image content the same way, so a
            // rejected or unsupported request should surface as a clear message rather
            // than an unhandled exception that crashes the whole session.
            ChatMessageContent response;
            try
            {
                response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nThe model rejected this image request: {ex.Message}");
                return;
            }

            // 5. Output the visual analysis
            Console.WriteLine("\n--- VISIONARY ANALYSIS ---");
            Console.WriteLine(response.Content ?? "(The model returned no analysis for this image.)");
        }
    }
}