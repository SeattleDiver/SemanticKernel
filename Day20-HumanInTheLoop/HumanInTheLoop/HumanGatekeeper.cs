// HumanGatekeeper
// ---------------------------------------------------------------------------
// The hard stop the AI cannot bypass: blocks on console input and only
// returns IsApproved = true for an exact "APPROVED" match. Anything else is
// treated as feedback and routed back to the AI for another draft.
namespace HumanInTheLoop
{
    /// <summary>
    /// Represents the result of a human review.
    /// </summary>
    /// <param name="IsApproved">Whether the human explicitly approved the draft.</param>
    /// <param name="Feedback">The human's feedback text, or "APPROVED" when <paramref name="IsApproved"/> is true.</param>
    public record ReviewResult(bool IsApproved, string Feedback);

    /// <summary>
    /// Manages console I/O to securely halt the application until a human makes a decision.
    /// </summary>
    internal class HumanGatekeeper
    {
        /// <summary>Prints the draft to the console and blocks until a human types "APPROVED" or provides feedback.</summary>
        /// <param name="draft">The AI-generated draft to present for review.</param>
        /// <returns>A <see cref="ReviewResult"/> indicating approval, or rejection carrying the human's feedback text.</returns>
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
