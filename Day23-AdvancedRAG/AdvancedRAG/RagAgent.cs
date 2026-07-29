using Microsoft.SemanticKernel;
#if CHATGPT
using Microsoft.SemanticKernel.Connectors.OpenAI;
#elif GOOGLE
using Microsoft.SemanticKernel.Connectors.Google;
#endif


namespace AdvancedRAG
{
    /// <summary>
    /// The conversational AI that uses the retrieved documents to answer questions.
    /// </summary>
    internal class RagAgent
    {
        private readonly Kernel _kernel;
        private readonly HybridRetriever _retriever;

        public RagAgent(Kernel kernel, HybridRetriever retriever)
        {
            _kernel = kernel;
            _retriever = retriever;
        }

        public async Task<string> AnswerAsync(string question)
        {
            // 1. Retrieve the hybrid context from our mock database
            var contexts = await _retriever.SearchAsync(question);
            string combinedContext = string.Join("\n- ", contexts);

            // 2. Build the strig RAG prompt
            string prompt = $@"
                You are an expert technical assistant.
                Answer the user's question using ONLY the provide CONTEXT.
                If the answer is not in the context, output exactly: 'Data not found'.

                CONTEXT:
                - {combinedContext}

                QUESTION: {question}
                ";

            // 3. Execute with low temperature for factual grounding
#if CHATGPT
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                Temperature = 0.0
            };
#elif GOOGLE
            var executionSettings = new GeminiPromptExecutionSettings
            {
                Temperature = 0.0
            };
#endif

            var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(executionSettings));

            return result.ToString();
        }
    }
}
