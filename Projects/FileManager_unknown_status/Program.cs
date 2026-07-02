using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.Google;

namespace DataAnalyst
{
    public class ExpenseReport
    {
        // Step 1: Define the target C# class for our extracted data
        [JsonPropertyName("employeeName")]
        public string EmployeeName { get; set; } = string.Empty;

        [JsonPropertyName("company")]
        public string Company { get; set; } = string.Empty;

        [JsonPropertyName("destination")]
        public string Destination { get; set; } = string.Empty;

        [JsonPropertyName("travelDate")]
        public string TravelDate { get; set; } = string.Empty;

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            // Step 2: Initialize the Kernel with Gemini 2.5 flash
            var builder = Kernel.CreateBuilder();

            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                            ?? throw new Exception("GEMINI_API_KEY environment variable not set");
            string model = "gemini-2.5-flash";

            builder.AddGoogleAIGeminiChatCompletion(model, apiKey);
            Kernel kernel = builder.Build();

            // Step 3: The unstructured data (a message call)
            string rawEmail = @"
Hey accounting team, it'm Michael Scott from Dunder Mifflin.
I'm sending in my expense report for my recent corporate trip to New York.
The trip was on October 14th, 2024.
I spent $15 on a hot dog, $45 on a taxi, and $120 for a hotel room.
The total comes to $180.
Please reimburse my bank account as soon as possible.  Thanks!
            ";

            // Step 4: The Extraction Prompt
            string promptTemplate = @"
You are a precise data extraction agent.  Extract the following informaitno from the email below.
Return a JSON object with exactly these keys:
- employeeName (string)
- company (string)
- destination (string)
- travelDate (string)
- totalAmount (number)

Email Content:
{{$emailText}}
";

            // Step 5: Configure Gemini to output string JSON
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0,          // Zero temperature prevents hallucinations
                ResponseMimeType = "application/json"   // Magic setting to guarantee JSON output
            };

            var arguments = new KernelArguments(executionSettings)
            {
                { "emailText", rawEmail }
            };

            Console.WriteLine("Incoming Unstructured Email:");
            Console.WriteLine(rawEmail.Trim());
            Console.WriteLine("Agent is extracting data to a C# object...");

            // Step 6: Execute the prompt
            var result = await kernel.InvokePromptAsync(promptTemplate, arguments);

            // Step 7: Parese the AI response directly into our C# class
            string jsonResponse = result.ToString();

            try
            {
                // Deserialize the JSON string into our stringly-typed ExpenseReport object
                ExpenseReport? report = JsonSerializer.Deserialize<ExpenseReport>(jsonResponse);
                if (report != null)
                {
                    Console.WriteLine("--- SUCCESSFULLY EXTRACTED C# OBJECT ---");
                    Console.WriteLine($"Employee:    {report.EmployeeName}");
                    Console.WriteLine($"Company:     {report.Company}");
                    Console.WriteLine($"Destination: {report.Destination}");
                    Console.WriteLine($"Date:        {report.TravelDate}");
                    Console.WriteLine($"Amount:      ${report.TotalAmount}");
                }

                // You could easily save this to a database
                // dbContext.ExpenseReports.Add(report);
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                Console.WriteLine($"Raw AI Output was:\n{jsonResponse}");
            }
        }
    }
}
