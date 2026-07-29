// Day 3: The Format Translator
// ---------------------------------------------------------------------------
// A "Transformation Agent" whose only job is turning messy natural language
// into strict, machine-readable JSON. Demonstrates aggressive negative
// prompting (telling the model what NOT to do) plus GeminiPromptExecutionSettings
// (Temperature/TopP) to suppress the model's "creativity" so it behaves like
// a deterministic parser instead of a conversational partner.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace FormatTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 1. Init the kernel with Gemini 2.5 flash
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
            Kernel kernel = builder.Build();

            // Step 2. Defind the unstrucured input and the prompt template
            string unstructuredData = "We just hired Sarah Connor. She is 29 years old, works as a Cybersecurity Analyst, and her email is sarah.c@sky.net. She starts on Monday.";

            // Step 3. Define the strict prompt template
            // We explicitly ban markdown blocks and converssational text to ensure raw JSON output
            string promptTemplate = @"
System: You are a strict data transformation agent. Your ONLY job is to convert natural language text into a valid JSON object. 
You must NOT output any markdown formatting (like ```json), no conversational text, and no explanations. Output raw JSON only.

The JSON must have the exact following keys:
- FirstName (string)
- LastName (string)
- Age (integer)
- JobTitle (string)
- Email (string)

User: Translate this text into JSON:
{{$input}}
";
            // Step 4. Configure Execution Settings to eliminate creativity
            var executionSettings = new GeminiPromptExecutionSettings()
            {
                Temperature = 0.0, // Eliminate randomness
                TopP = 0.1
            };

            // Step 5. Execute the prompt with the unstructured input
            var arguments = new KernelArguments(executionSettings)
            {
                {  "input", unstructuredData }
            };

            Console.WriteLine("Input Data:");
            Console.WriteLine(unstructuredData);
            Console.WriteLine("Transformatin natural language to JSON...");

            // Step 6. Execute the prompt
            var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

            // Step 7. Output the result
            Console.WriteLine("--- STRING JSON OUTPUT ---");
            Console.WriteLine(result.ToString());
            Console.WriteLine("--------------------------");
        }
    }
}