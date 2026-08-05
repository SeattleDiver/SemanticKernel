// Day 6: The File Manager
// ---------------------------------------------------------------------------
// Gives the agent "hands": a plugin that can list, read, and write files on
// the local filesystem - the first episode with real side-effects instead of
// just generated text. The AI is handed a vague, multi-step goal ("find the
// log file, summarize the errors, write the summary") and has to chain
// ListFiles -> ReadFile -> WriteFile on its own via auto tool-invocation.
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace Day6FileManager
{
    // Step 1: Define the File System Plugin
    public class FileSystemPlugin
    {
        private readonly string _currentDirectory;

        public FileSystemPlugin()
        {
            // Sandbox the AI to the application's current running directory for safety.
            // Side-effect-capable plugins must restrict what the model can touch -
            // without this, a malicious prompt could read or delete files well
            // outside what this demo intends to expose.
            _currentDirectory = Directory.GetCurrentDirectory();
        }

        // Step 1b: Resolve a requested file name to a real path that is
        // guaranteed to stay inside the sandbox. Path.Combine alone does not
        // stop something like "../secrets.txt" or an absolute path from
        // escaping _currentDirectory - we have to resolve the full path and
        // verify it still lives under the sandbox root before touching disk.
        private bool TryResolveSandboxedPath(string fileName, out string resolvedPath, out string error)
        {
            string sandboxRoot = Path.GetFullPath(_currentDirectory) + Path.DirectorySeparatorChar;
            string fullPath = Path.GetFullPath(Path.Combine(_currentDirectory, fileName));

            if (!fullPath.StartsWith(sandboxRoot, StringComparison.OrdinalIgnoreCase))
            {
                resolvedPath = string.Empty;
                error = $"Error: '{fileName}' resolves outside the sandboxed directory and was blocked.";
                return false;
            }

            resolvedPath = fullPath;
            error = string.Empty;
            return true;
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
            if (!TryResolveSandboxedPath(fileName, out string path, out string sandboxError)) return sandboxError;
            if (!File.Exists(path)) return $"Error: the file '{fileName}' does not exist.";

            try
            {
                return await File.ReadAllTextAsync(path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // A locked, permission-denied, or otherwise unreadable file must not
                // throw uncaught here - that would crash the whole chat loop instead
                // of letting the AI see and react to a normal tool failure.
                return $"Error: could not read '{fileName}' ({ex.Message}).";
            }
        }

        [KernelFunction("WriteFile")]
        [Description("Writes text content into a specified file.  The the file doesn't exist, it will be created")]
        public async Task<string> WriteFileAsync(
            [Description("The name of the file to write to (e.g. 'summary.txt')")] string fileName,
            [Description("The text content to save inside the file")] string content
            )
        {
            Console.WriteLine($"[PLUGIN EXECUTING] AI is writing data to file: '{fileName}'...");
            if (!TryResolveSandboxedPath(fileName, out string path, out string sandboxError)) return sandboxError;

            try
            {
                await File.WriteAllTextAsync(path, content);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // Same reasoning as ReadFileAsync: report the disk failure back to the
                // AI as a tool result rather than letting it crash the whole session.
                return $"Error: could not write '{fileName}' ({ex.Message}).";
            }

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
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY environment variable not set.");
            string model = "gemini-2.5-flash";

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion(model, apiKey);

            // Step 4: Add our file system plugin
            builder.Plugins.AddFromObject(new FileSystemPlugin(), "FileSystem");

            Kernel kernel = builder.Build();

            // Step 5: Define a multi-step task for the AI
            string prompt = "Look in the current directory for a server log file.  Read its contents, figure out what errors occurred, and write a summary of those errors into a new file called 'error_summary.txt'.";

            // Step 6: Enable Auto-Invocation so the AI can chain the tools
            var executionSettings = new GeminiPromptExecutionSettings
            {
                ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
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