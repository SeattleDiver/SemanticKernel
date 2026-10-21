// Day 14: Document QA
// ---------------------------------------------------------------------------
// Grounded question-answering via full-context stuffing: the entire "manual"
// is pasted directly into the prompt on every turn, and the model is told to
// answer only from it (and admit when it can't). No retrieval is involved -
// this is the brute-force contrast to the retrieval-based RAG approach shown
// in Day 8 and Day 23.
using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace DocumentQA
{
    /// <summary>Entry point that answers questions grounded in a full document pasted directly into the prompt each turn.</summary>
    class Program
    {
        /// <summary>Loads a sample technical manual, then loops on user questions, answering only from that document.</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // 1. Setup the Kernel
            var builder = Kernel.CreateBuilder();
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new Exception("OPENAI_API_KEY was not found");

            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
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
            Console.WriteLine("Document Loaded (simulated large context)");
            Console.WriteLine("Ask a question about the Quantum-X system (or type 'exit').\n");

            while (true)
            {
                Console.Write("User: ");
                string? question = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(question) || question.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

                // 4. Execution settings
                // we keep temperature low (0.0) for factual QA tasks.
                var executionSettings = new OpenAIPromptExecutionSettings { Temperature = 0.0 };
                var arguments = new KernelArguments(executionSettings)
                {
                    { "documentContent", longDocument },
                    { "question", question }
                };

                // 5. Invoke prompt
                // A transient failure here (network hiccup, rate limit, etc.) should not
                // kill the whole console session - report it and let the student keep asking.
                try
                {
                    var result = await kernel.InvokePromptAsync(promptTemplate, arguments);
                    Console.WriteLine($"\nAI: {result}\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[Error] The request failed: {ex.Message}\n");
                }
            }

        }

        /// <summary>Returns a fixed sample technical manual (fictional "Quantum-X" device) for the QA loop to answer from.</summary>
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
