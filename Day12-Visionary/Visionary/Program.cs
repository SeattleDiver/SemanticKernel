using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Visionary
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 1. Initialize the Kernel with a vision-capable OpenAI chat model
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
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
            var settings = new OpenAIPromptExecutionSettings { Temperature = 0.4 };

            var response = await chatService.GetChatMessageContentAsync(chatHistory, settings, kernel);

            // 5. Output the visual analysis
            Console.WriteLine("\n--- VISIONARY ANALYSIS ---");
            Console.WriteLine(response.Content); 
        }
    }
}