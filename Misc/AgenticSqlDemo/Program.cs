using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

// 1. Setup the Database
var dbConnection = DatabaseSetup.InitializeMockDatabase();

// 2. Initialize the Kernel Builder
var builder = Kernel.CreateBuilder();

// Use Google Gemini
string apiKey = "AIzaSyDgFlUVWYNvW0i2znbEykNeS154B9K85yg";
builder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-3-flash-preview",
    apiKey: apiKey
);

// 3. Register our Custom Plugin
builder.Plugins.AddFromObject(new DatabasePlugin(dbConnection), "SqlDatabase");

var kernel = builder.Build();

// 4. Configure the LLM to automatically call our C# functions
var executionSettings = new GeminiPromptExecutionSettings
{
    ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions
};

// 5. Setup the System Prompt / Chat History
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
var chatHistory = new ChatHistory("You are a helpful database assistant. You convert user questions into SQL, execute them, and return user-friendly answers.");

Console.WriteLine("SQL Analytics Agent initialized. Type 'exit' to quit.\n");

// 6. The Agentic Loop
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("User > ");
    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput)) continue;
    if (userInput.ToLower() == "exit") break;

    Console.ForegroundColor = ConsoleColor.Gray;
    Console.WriteLine("Thinking... (and potentially running SQL)...");

    chatHistory.AddUserMessage(userInput);

    // This single line handles the entire agent loop:
    // It calls the LLM -> LLM asks for schema -> SK runs GetSchema() -> SK sends schema to LLM -> 
    // LLM writes SQL -> SK runs ExecuteQuery() -> SK sends data to LLM -> LLM formats final answer.
    var response = await chatCompletionService.GetChatMessageContentAsync(
        chatHistory,
        executionSettings,
        kernel
    );

    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\nAgent > {response.Content}\n");

    // Add agent response to history so it remembers context
    if (!string.IsNullOrEmpty(response.Content))
    {
        chatHistory.AddAssistantMessage(response.Content);
    }
}


















/*
using Microsoft.SemanticKernel;

System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
sw.Start();

Console.WriteLine($"Creating Kernel, {sw.ElapsedMilliseconds}");
var builder = Kernel.CreateBuilder();

// Use the Google Gemini Connector
// NuGet package: Microsoft.SemanticKernel.Connectors.Google
builder.AddGoogleAIGeminiChatCompletion(
    modelId: "gemini-2.5-flash-lite", 
    apiKey: "AIzaSyDgFlUVWYNvW0i2znbEykNeS154B9K85yg"
);

Kernel kernel = builder.Build();

Console.WriteLine($"Invoking prompt, {sw.ElapsedMilliseconds}");
var response = await kernel.InvokePromptAsync("Write a C# class to generate a 16 characater cryptographically secure random string.");

Console.WriteLine($"Reponse available, {sw.ElapsedMilliseconds}");
Console.WriteLine(response);
*/