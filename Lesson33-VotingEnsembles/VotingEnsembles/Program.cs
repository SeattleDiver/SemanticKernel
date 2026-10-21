// Day 33: Voting & Self-Consistency Ensembles
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

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
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");

            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion("gpt-4.1-mini", apiKey);
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

            // A parse failure isn't a dropped connection - it's a returned-but-unusable sample.
            // Exclude it the same way, so a malformed response can't cast a real vote for the
            // literal string "UNPARSEABLE".
            ReasoningSample[] votableSamples = samples.Where(s => s.FinalAnswer != "UNPARSEABLE").ToArray();
            int excludedCount = samples.Length - votableSamples.Length;
            if (excludedCount > 0)
            {
                Console.WriteLine($"[WARN] {excludedCount} sample(s) returned unparseable JSON and were excluded from voting.\n");
            }

            if (votableSamples.Length == 0)
            {
                Console.WriteLine("\nAll samples were unparseable - no consensus can be computed.");
                return;
            }

            if (votableSamples.Length < SampleCount)
            {
                Console.WriteLine($"Tally is based on {votableSamples.Length} of {SampleCount} requested samples.\n");
            }

            ConsensusResult consensus = ConsensusVoter.Tally(votableSamples);

            Console.WriteLine("--- Vote Tally ---");
            foreach (var (answer, votes) in consensus.VoteCounts.OrderByDescending(kv => kv.Value))
            {
                Console.WriteLine($"  {answer}: {votes} vote(s)");
            }

            if (consensus.IsTie)
            {
                Console.WriteLine($"\nTIE - no single answer received a majority of {votableSamples.Length} samples.");
                Console.WriteLine("\n--- NO CONSENSUS (showing one tied answer) ---");
            }
            else
            {
                Console.WriteLine("\n--- CONSENSUS ANSWER ---");
            }

            Console.WriteLine(consensus.WinningAnswer);
        }
    }
}
