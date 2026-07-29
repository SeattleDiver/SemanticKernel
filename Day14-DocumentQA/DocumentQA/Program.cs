using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif

namespace DocumentQA
{
    class Program
    {
        static async Task Main(string[] args)
        {
#if !CHATGPT && !GOOGLE
            throw new InvalidOperationException(
                "No LLM provider selected. Define either CHATGPT or GOOGLE " +
                "(see <DefineConstants> in DocumentQA.csproj) before building.");
#else
            // 1. Setup the Kernel
            var builder = Kernel.CreateBuilder();
#if CHATGPT
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY was not found");
            string modelId = Environment.GetEnvironmentVariable("OPENAI_CHAT_MODEL") ?? "gpt-4o-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);
#elif GOOGLE
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new Exception("GEMINI_API_KEY was not found");

            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
#endif
            Kernel kernel = builder.Build();

            // 2. Simulate or load a large document
            // In a real scenario, use File.ReadAllTextAsync("huge_document.txt");
            string longDocument = GenerateSampleTechnicalDoc();

            // 3. Define QA prompt template
            // We instruct the model that the document is the "source of truth"
            string promptTemplate = @"
System: You are an expert Technical Document Analyst.
Below is a technical manual for the 'Quantum-X' system.
Use the provided manual to answer user questions.
If the answer is not in the manual, state that you do not know.

--- BEGIN MANUAL ---
{{$documentContent}}
--- END MANUAL ---

User Question: {{$question}}

Assistant: (Cite the section number in your answer)
";
            Console.WriteLine("Document Loaded (simulated large context");
            Console.WriteLine("Ask a question about the Quantum-X system (or type 'exit').\n");

            while (true)
            {
                Console.Write("User: ");
                string? question = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // 4. Execution settings
                // we keep temperature low (0.0) for factual QA tasks.
#if CHATGPT
                var executionSettings = new OpenAIPromptExecutionSettings { Temperature = 0.0 };
#elif GOOGLE
                var executionSettings = new GeminiPromptExecutionSettings { Temperature = 0.0 };
#endif
                var arguments = new KernelArguments(executionSettings)
                {
                    { "documentContent", longDocument },
                    { "question", question }
                };

                // 5. Invoke prompt
                var result = await kernel.InvokePromptAsync(promptTemplate, arguments);
                Console.WriteLine($"\nAI: {result}\n");
            }
#endif
        }

        static string GenerateSampleTechnicalDoc()
        {
            return @"
            SECTION 1: OVERVIEW
            The Quantum-X is a liquid-cooled processing unit. It operates at 2 Kelvin.
            
            SECTION 2: SAFETY
            Warning: Do not open the cooling chamber while the blue light is flashing.
            In case of emergency, press the red 'HALT' button located on the rear panel.
            
            SECTION 3: TROUBLESHOOTING
            Error Code E1: Low coolant levels. Refill with Grade-A Helium.
            Error Code E2: Temperature fluctuation. Calibrate the thermal sensors.
            ";
        }
    }
}
