using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace FileManager
{
    // Step 1: Define the File System Plugin
    /// <summary>Native C# plugin that lets the model list, read, and write files, sandboxed to the app's current directory.</summary>
    public class FileSystemPlugin
    {
        private readonly string _currentDirectory;

        /// <summary>Captures the current working directory as the sandbox root for every file operation this plugin exposes.</summary>
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
        /// <summary>Resolves a requested file name to a full path and verifies it still falls within the sandbox root.</summary>
        /// <param name="fileName">The file name or relative path requested by the model.</param>
        /// <param name="resolvedPath">The resolved absolute path, if it stayed inside the sandbox; otherwise empty.</param>
        /// <param name="error">A description of why resolution failed, if it did; otherwise empty.</param>
        /// <returns><c>true</c> if <paramref name="fileName"/> resolved to a path inside the sandbox; otherwise <c>false</c>.</returns>
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

        /// <summary>Lists the names of all files in the sandboxed current directory.</summary>
        /// <returns>A newline-separated list of file names, or a message noting the directory is empty.</returns>
        [KernelFunction("ListFiles")]
        [Description("Lists the names of all the files in the current directory.")]
        public string ListFiles()
        {
            Console.WriteLine("[PLUGIN EXECUTING] AI is listing files in the directory...");
            var files = Directory.GetFiles(_currentDirectory);
            if (files.Length == 0) return "The directory is empty";

            return string.Join("\n", files.Select(Path.GetFileName));
        }

        /// <summary>Reads and returns the text content of a file inside the sandbox.</summary>
        /// <param name="fileName">The exact name of the file to read.</param>
        /// <returns>The file's text content, or a descriptive error string if it is missing, outside the sandbox, or unreadable.</returns>
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // A locked, permission-denied, or otherwise unreadable file must not
                // throw uncaught here - that would crash the whole chat loop instead
                // of letting the AI see and react to a normal tool failure.
                return $"Error: could not read '{fileName}' ({ex.Message}).";
            }
        }

        /// <summary>Writes text content into a file inside the sandbox, creating it if it doesn't already exist.</summary>
        /// <param name="fileName">The name of the file to write to.</param>
        /// <param name="content">The text content to save inside the file.</param>
        /// <returns>A success message, or a descriptive error string if the path is outside the sandbox or unwritable.</returns>
        [KernelFunction("WriteFile")]
        [Description("Writes text content into a specified file. If the file doesn't exist, it will be created")]
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
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                // Same reasoning as ReadFileAsync: report the disk failure back to the
                // AI as a tool result rather than letting it crash the whole session.
                return $"Error: could not write '{fileName}' ({ex.Message}).";
            }

            return $"Success: The file '{fileName}' was successfully created and written";
        }
    }
}
