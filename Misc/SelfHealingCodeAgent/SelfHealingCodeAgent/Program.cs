using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Kernel.CreateBuilder();

string apiKey = "AIzaSyDgFlUVWYNvW0i2znbEykNeS154B9K85yg";
builder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-3.1-flash-lite",
    apiKey: apiKey
);

var kernel = builder.Build();
var chatService = kernel.GetRequiredService<IChatCompletionService>();

var chatHistory = new ChatHistory(
    "You are a Senior C# Developer. You are coding for a .NET 8.0 environment. " +
    "When generating code, you MUST include the appropriate 'using' statements at the top. " +
    "If the code fails to compile, you will receive compiler errors; your job is to analyze " +
    "the error, fix the specific line, and ensure all necessary references are satisfied."
);

Console.Write("Enter a C# task (e.g. 'Write a static class that validates a credit card number'):\n User > ");
string userTask = Console.ReadLine();
if (string.IsNullOrWhiteSpace(userTask)) userTask = "Write a static class that reverses a string";

chatHistory.AddUserMessage(userTask);

// Add this snippet before your loop in Program.cs
var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies()
    .Where(a => !a.IsDynamic)
    .Select(a => a.GetName().Name)
    .ToList();

chatHistory.AddSystemMessage($"Available namespaces/assemblies you can use: {string.Join(", ", loadedAssemblies)}");

int maxRetries = 3;
int currentAttempt = 1;
bool isCompiled = false;

while (currentAttempt <= maxRetries && !isCompiled)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\nAttempt {currentAttempt}/{maxRetries}: Generating code with Gemini...");
     
    var response = await chatService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
    string llmReply = response.Content ?? "";

    chatHistory.AddAssistantMessage(llmReply);

    string rawCode = CodeExtractor.ExtractCSharpCode(llmReply);
    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("Code generated.  Attempting to compile...");
    Console.ForegroundColor= ConsoleColor.Green;
    Console.WriteLine(rawCode);

    var compilationResult = CompilerEngine.AttemptCompilation(rawCode);
    if (compilationResult.IsSuccess)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nSuccess!  The code compiled successfully.");
        Console.WriteLine("======================= Final Code ======================= ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(rawCode);
        Console.ResetColor();
        isCompiled = true;
    }
    else
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("Compilation failed with the following errors:\n");
        Console.WriteLine(compilationResult.Message);

        Console.WriteLine("\nRetrying with feedback to Gemini...\n");
        // Add the compilation errors back to the chat history for Gemini to learn from
        chatHistory.AddUserMessage($"The code you generated failed to compile with the following errors:\n{compilationResult.Message}\nPlease fix these issues and provide the corrected code.");
        currentAttempt++;
    }
}

if (!isCompiled)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"\nFailed to generate compilable code after {maxRetries} attempts. Please try again later.");
}

Console.ResetColor();