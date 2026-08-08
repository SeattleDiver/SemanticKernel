// Day 33: Voting & Self-Consistency Ensembles
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace VotingEnsembles
{
    /// <summary>
    /// Entry point that samples a question multiple times and aggregates the answers by majority vote.
    /// </summary>
    internal class Program
    {
        private const int SampleCount = 5;

        /// <summary>
        /// Draws <see cref="SampleCount"/> independent samples for a fixed problem and tallies the votes.
        /// </summary>
        static async Task Main(string[] args)
        {
            string apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? throw new InvalidOperationException("GEMINI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddGoogleAIGeminiChatCompletion("gemini-2.5-flash", apiKey);
            Kernel kernel = builder.Build();

            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var sampler = new ReasoningSampler(chatService);

            const string question =
                "A bat and a ball cost $1.10 in total. The bat costs $1.00 more than the ball. " +
                "How much does the ball cost?";

            Console.WriteLine($"PROBLEM: {question}\n");

            // Each sample is isolated: a single dropped connection shouldn't crash a vote
            // that four other samples completed fine - it should just lose one vote.
            var sampleTasks = Enumerable.Range(0, SampleCount).Select(async i =>
            {
                try
                {
                    return await sampler.SampleAsync(question);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Sample {i + 1} failed and was excluded from voting: {ex.Message}");
                    return null;
                }
            });
            ReasoningSample?[] rawSamples = await Task.WhenAll(sampleTasks);
            ReasoningSample[] samples = rawSamples.Where(s => s is not null).Select(s => s!).ToArray();

            if (samples.Length == 0)
            {
                Console.WriteLine("\nAll samples failed - no consensus can be computed.");
                return;
            }

            for (int i = 0; i < samples.Length; i++)
            {
                Console.WriteLine($"--- Sample {i + 1} (answer: {samples[i].FinalAnswer}) ---\n{samples[i].Reasoning}\n");
            }

            if (samples.Length < SampleCount)
            {
                Console.WriteLine($"Tally is based on {samples.Length} of {SampleCount} requested samples.\n");
            }

            ConsensusResult consensus = ConsensusVoter.Tally(samples);

            Console.WriteLine("--- Vote Tally ---");
            foreach (var (answer, votes) in consensus.VoteCounts.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"  {answer}: {votes} vote(s)");
            }

            if (consensus.IsTie)
            {
                Console.WriteLine($"\nTIE - no single answer received a majority of {SampleCount} samples.");
            }

            Console.WriteLine($"\n--- CONSENSUS ANSWER ---\n{consensus.WinningAnswer}");
        }
    }
}
