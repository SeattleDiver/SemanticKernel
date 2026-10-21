namespace Evaluator
{
    /// <summary>A single test case: a question to ask the target agent and the known-correct answer to judge it against.</summary>
    /// <param name="Question">The question to ask the target agent.</param>
    /// <param name="GroundTruth">The known-correct answer the judge compares the response against.</param>
    public record TestCase(string Question, string GroundTruth);
}
