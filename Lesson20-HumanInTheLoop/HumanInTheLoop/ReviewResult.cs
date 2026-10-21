namespace HumanInTheLoop
{
    /// <summary>
    /// Represents the result of a human review.
    /// </summary>
    /// <param name="IsApproved">Whether the human explicitly approved the draft.</param>
    /// <param name="Feedback">The human's feedback text, or "APPROVED" when <paramref name="IsApproved"/> is true.</param>
    public record ReviewResult(bool IsApproved, string Feedback);
}
