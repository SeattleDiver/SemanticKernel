namespace HumanInTheLoop
{
    /// <summary>
    /// Represents the result of a human review.
    /// </summary>
    public record ReviewResult(bool IsApproved, string Feedback);

    /// <summary>
    /// Manages console I/O to securely halt the application until a human makes a decision.
    /// </summary>
    internal class HumanGatekeeper
    {
        public ReviewResult ReviewDraft(string draft)
        {
            Console.WriteLine("\n========================================");
            Console.WriteLine("🤖 AI DRAFT PROPOSAL:");
            Console.WriteLine("----------------------------------------");
            Console.WriteLine(draft);
            Console.WriteLine("========================================");

            Console.WriteLine("\n🛑 HUMAN REVIEW REQUIRED");
            Console.WriteLine("Type 'APPROVED' to finalize the document.");
            Console.WriteLine("Otherwise, type your feedback to request changes from the AI:");
            Console.Write("> ");

            string? input = Console.ReadLine();

            // Check if the human explicitly typed the approval keyword
            if (!string.IsNullOrWhiteSpace(input) && input.Trim().Equals("APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                return new ReviewResult(true, "APPROVED");
            }

            // If not approved, return the human's input as constructive feedback
            return new ReviewResult(false, input ?? "Please rewrite the draft.");
        }
    }
}
