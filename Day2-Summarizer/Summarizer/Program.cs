using Microsoft.SemanticKernel;

namespace Summarizer
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            // Step1: Initialize the Kernel
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("GEMINI_API_KEY environment variable not set.");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
            Kernel kernel = builder.Build();

            // Step 2. Create text file for demonstration
            string filePath = "article.txt";
            CreateDummyArticle(filePath);

            Console.WriteLine("Reading article from file...");
            string textToSummarize = await File.ReadAllTextAsync(filePath);

            // Step 3. Define the Prompt Template
            // We use the {{$variableName}} syntax to indicate where the input text should be inserted in the prompt.
            string promptTemplate = @"
                Read the following text and summarize it into exactly 3 bullet points.
                Focus on the main ideas and ingore minor details.

                TEXT TO SUMMARIZE:
                {{$articleContent}}

                SUMMARY:
            ";

            // Step 4: Map the variables to the prompt template
            var aguments = new KernelArguments
            {
                { "articleContent", textToSummarize }
            };

            Console.WriteLine("Generating summary...");

            // Step 5: Execute the prompt
            var result = await kernel.InvokePromptAsync(promptTemplate, aguments);

            // Step 6: Display the result
            Console.WriteLine("--- AI Summary ---");
            Console.WriteLine(result.ToString());
            Console.WriteLine("------------------");

        }

        // Helper method to generate a long text file for our demo
        static void CreateDummyArticle(string path)
        {
            string content = @"
                Quantum computing is a rapidly emerging technology that harnesses the laws of quantum mechanics to solve problems too complex for classical computers. 
                Today, IBM Quantum makes real quantum hardware available to hundreds of thousands of developers. Engineers deliver regular updates to increasingly powerful 
                quantum systems and software. Classical computers, which include smartphones and laptops, encode information in binary 'bits' that can either be 0s or 1s. 
                In a quantum computer, the basic unit of memory is a quantum bit or qubit. Qubits are made using physical systems, such as the spin of an electron or 
                the orientation of a photon. Because of quantum properties like superposition and entanglement, a quantum computer can explore multiple solutions to a 
                problem simultaneously, making it exponentially faster for specific tasks like cryptography, material science, and complex optimization problems. 
                However, quantum computers are highly sensitive to their environment. Heat, electromagnetic fields, and collisions with air molecules can cause a qubit 
                to lose its quantum properties, a process known as decoherence.
            ";
            File.WriteAllText(path, content.Trim());
        }
    }
}