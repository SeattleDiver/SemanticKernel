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
            // For this example, we use a public URL of a sample chart/image
            string imageUrl = "https://hustleescape.com/wp-content/uploads/2020/06/Financial-Independence-Chart-1024x528.png";
            Console.WriteLine($"Analyzing Image {imageUrl}");

            var messageItems = new ChatMessageContentItemCollection
            {
                new TextContent("Analyze this image.  Identify the type of chart, the key data points, and summarize the main trend."),
                new ImageContent(new Uri(imageUrl)) { MimeType = "image/png" }
            };

            chatHistory.AddUserMessage(messageItems);

            // 4. Invoke the model
            // Multimodal tasks often benefit from a slightly higher temperature to encourage creative descriptions
            var settings = new GeminiPromptExecutionSettings { Temperature = 0.4 };

            var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);

            // 5. Output the visual analysis
            Console.WriteLine("\n--- VISIONARY ANALYSIS ---");
            Console.WriteLine(response.Content); 
        }
    }
}