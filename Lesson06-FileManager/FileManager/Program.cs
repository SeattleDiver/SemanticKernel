// Day 6: The File Manager
// ---------------------------------------------------------------------------
// Gives the agent "hands": a plugin that can list, read, and write files on
// the local filesystem - the first episode with real side-effects instead of
// just generated text. The AI is handed a vague, multi-step goal ("find the
// log file, summarize the errors, write the summary") and has to chain
// ListFiles -> ReadFile -> WriteFile on its own via auto tool-invocation.
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace FileManager
{
    /// <summary>Entry point that hands the model a vague, multi-step file task and lets it chain tool calls to complete it.</summary>
    class Program
    {
        /// <summary>Seeds a dummy log file, then asks the model to find it, summarize its errors, and write the summary to disk.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 2: Setup the Dummy Log File for the AI to find
            SetupDummyLogFile();

            // Step 3: Initialize the Kernel with our model
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY environment variable not set.");
            string model = "gpt-4.1-mini";

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
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                Temperature = 0.0
            };

            var arguments = new KernelArguments(executionSettings);

            Console.WriteLine($"Task: {prompt}");
            Console.WriteLine("Agent is thinking and interacting with the file system...");

            // Step 7: Execute the prompt. Wrapped in try/catch because this is
            // the network call to the model - a bad key, rate limit, or
            // connectivity blip would otherwise crash the whole program with
            // a raw stack trace instead of a readable message.
            try
            {
                var result = await kernel.InvokePromptAsync(prompt, arguments);

                // Step 8: Display the final result
                Console.WriteLine("--- AI TASK REPORT ---");
                Console.WriteLine(result.ToString().Trim());
                Console.WriteLine("----------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Task failed: {ex.Message}");
            }

            // Verify the file was actually created on the hard drive
            string summaryPath = Path.Combine(Directory.GetCurrentDirectory(), "error_summary.txt");
            if (File.Exists(summaryPath))
            {
                Console.WriteLine("[VERIFICATION] Reading 'error_summary.txt' directly from the local filesystem");
                Console.WriteLine(await File.ReadAllTextAsync(summaryPath));
            }

        }

        /// <summary>Writes a fixed demo server log (containing two errors) to disk for the agent to discover and summarize.</summary>
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