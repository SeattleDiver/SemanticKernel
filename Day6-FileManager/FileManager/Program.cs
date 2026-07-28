using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace Day6FileManager
{
    // Step 1: Define the File System Plugin
    public class FileSystemPlugin
    {
        private readonly string _currentDirectory;

        public FileSystemPlugin()
        {
            // Sandbox the AI to the application's current running directory for safety
            _currentDirectory = Directory.GetCurrentDirectory();
        }

        [KernelFunction("ListFiles")]
        [Description("Lists the names of all the files in the current directory.")]
        public string ListFiles()
        {
            Console.WriteLine("[PLUGIN EXECUTING] AI is listing files in the directory...");
            var files = Directory.GetFiles(_currentDirectory);
            if (files.Length == 0) return "The directory is empty";

            return string.Join("\n", files.Select(Path.GetFileName));
        }

        [KernelFunction("ReadFile")]
        [Description("Reads and returns the text content of a specified file")]
        public async Task<string> ReadFileAsync([Description("The exact name of the file to read (e.g. 'document.txt')")] string fileName)
        {
            Console.WriteLine($"[PLUGIN EXECUTING] AI is reading file: '{fileName}'...");
            string path = Path.Combine(_currentDirectory, fileName);
            if (!File.Exists(path)) return $"Error: the file '{fileName}' does not exist.";

            return await File.ReadAllTextAsync(path);
        }

        [KernelFunction("WriteFile")]
        [Description("Writes text content into a specified file.  The the file doesn't exist, it will be created")]
        public async Task<string> WriteFileAsync(
            [Description("The name of the file to write to (e.g. 'summary.txt')")] string fileName,
            [Description("The text content to save inside the file")] string content
            )
        {
            Console.WriteLine($"[PLUGIN EXECUTING] AI is writing data to file: '{fileName}'...");
            string path = Path.Combine(_currentDirectory, fileName);
            await File.WriteAllTextAsync(path, content);

            return $"Success: The file '{fileName}' was successfully created and written";
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 2: Setup the Dummy Log File for the AI to find
            SetupDummyLogFile();

            // Step 3: Initialize the Kernel with our model
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable not set.");
            string model = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(model, apiKey);

            // Step 4: Add our file system plugin
            builder.Plugins.AddFromObject(new FileSystemPlugin(), "FileSystem");

            Kernel kernel = builder.Build();

            // Step 5: Define a multi-step task for the AI
            string prompt = "Look in the current directory for a server log file.  Read its contents, figure out what errors occurred, and write a summary of those errors into a new file called 'error_summary.txt'.";

            // Step 6: Enable Auto-Invocation so the AI can chain the tools
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                Temperature = 0.0
            };

            var arguments = new KernelArguments(executionSettings);

            Console.WriteLine($"Task: {prompt}");
            Console.WriteLine("Agent is thinking and interacting with the file system...");

            // Step 7: Execute the prompt
            var result = await kernel.InvokePromptAsync(prompt, arguments);

            // Step 8: Display the final result
            Console.WriteLine("--- AI TASK REPORT ---");
            Console.WriteLine(result.ToString().Trim());
            Console.WriteLine("----------------------");

            // Verify the file was actually created on the hard drive
            string summaryPath = Path.Combine(Directory.GetCurrentDirectory(), "error_summary.txt");
            if (File.Exists(summaryPath))
            {
                Console.WriteLine("[VERIFICATION] Reading 'error_summary.txt' directly from the local filesystem");
                Console.WriteLine(await File.ReadAllTextAsync(summaryPath));
            }

        }

        // Helper method to generate a log file for our demonstration
        static void SetupDummyLogFile()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "server_logs.txt");
            string logContent = @"
[08:00:00] INFO - Server booted successfully.
[08:15:22] INFO - User admin logged in.
[08:45:01] ERROR - DatabaseConnectionException: Failed to connect to DB at 192.168.1.50. Timeout.
[09:10:00] INFO - Nightly backup started.
[09:12:44] ERROR - NullReferenceException in PaymentProcessingModule.cs line 42.
[10:00:00] INFO - Routine health check passed.
";
            File.WriteAllText(path, logContent.Trim());
        }
    }
}