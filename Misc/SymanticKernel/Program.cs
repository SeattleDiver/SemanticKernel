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