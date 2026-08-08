// RagAgent
// ---------------------------------------------------------------------------
// Takes whatever HybridRetriever finds and turns it into a grounded answer:
// low temperature plus an explicit "say 'Data not found'" instruction keep
// the model from making things up when the retrieved context doesn't
// actually contain the answer.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;


namespace AdvancedRAG
{
    /// <summary>
    /// The conversational AI that uses the retrieved documents to answer questions.
    /// </summary>
    internal class RagAgent
    {
        private readonly Kernel _kernel;
        private readonly HybridRetriever _retriever;

        /// <summary>Creates an agent that answers questions grounded in documents retrieved by the given retriever.</summary>
        /// <param name="kernel">The kernel used to generate answers.</param>
        /// <param name="retriever">The hybrid retriever used to fetch grounding context.</param>
        public RagAgent(Kernel kernel, HybridRetriever retriever)
        {
            _kernel = kernel;
            _retriever = retriever;
        }

        /// <summary>Retrieves hybrid-search context for a question and answers it using only that context.</summary>
        /// <param name="question">The user's question.</param>
        /// <returns>The model's grounded answer, or "Data not found" if the context doesn't cover the question.</returns>
        public async Task<string> AnswerAsync(string question)
        {
            // 1. Retrieve the hybrid context from our mock database
            var contexts = await _retriever.SearchAsync(question);
            string combinedContext = string.Join("\n- ", contexts);

            // 2. Build the string RAG prompt
            string prompt = $@"
                You are an expert technical assistant.
                Answer the user's question using ONLY the provided CONTEXT.
                If the answer is not in the context, output exactly: 'Data not found'.

                CONTEXT:
                - {combinedContext}

                QUESTION: {question}
                ";

            // 3. Execute with low temperature for factual grounding
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0
            };

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

            return result.ToString();
        }
    }
}
