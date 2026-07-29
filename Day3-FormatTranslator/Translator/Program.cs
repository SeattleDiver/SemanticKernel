using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace FormatTranslator
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in Translator.csproj) before building.");
#else
            // Step 1. Init the kernel with a chat model
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("OPENAI_API_KEY environment variable is not set.");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? throw new Exception("GEMINI_API_KEY environment variable is not set.");
            string modelId = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(modelId, apiKey);
#endif
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
#if CHATGPT
            var executionSettings = new OpenAIPromptExecutionSettings()
            {
                Temperature = 0.0, // Eliminate randomness
                TopP = 0.1
            };
#elif GOOGLE
            var executionSettings = new GeminiPromptExecutionSettings()
            {
                Temperature = 0.0, // Eliminate randomness
                TopP = 0.1
            };
#endif

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
#endif
        }
    }
}