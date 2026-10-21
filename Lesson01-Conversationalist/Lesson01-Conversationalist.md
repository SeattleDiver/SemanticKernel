# Day 1: The Conversationalist (Building a Chatbot)

## 1. Project Overview

In this foundational project, we build a continuous, terminal-based chatbot. Running the program starts an infinite loop that reads a line of text from the console, sends it to OpenAI's chat model through Semantic Kernel, prints the reply, and waits for the next line — until the user types "exit". The goal is to introduce the absolute core mechanics of Microsoft's Semantic Kernel: setting up the `Kernel`, connecting an AI model (OpenAI `gpt-4.1-mini`), and managing a multi-turn conversation using a `ChatHistory` object so the model has memory of what was already said.

## 2. Prerequisites & Setup

* Environment: .NET 10 SDK installed.
* Project Type: A standard C# Console Application (`dotnet new console`).
* NuGet Packages required:

- Microsoft.SemanticKernel.Core (the core SDK)

- Microsoft.SemanticKernel.Connectors.OpenAI (the OpenAI connector)

* API Key: An OpenAI API key.
* Note: For security, the API key is read from the `OPENAI_API_KEY` environment variable rather than being hard-coded. If the variable is missing, the program throws an `InvalidOperationException` immediately instead of failing later with a confusing API error. The model used is `gpt-4.1-mini`.

## 3. Core Concepts Covered

* **The Kernel Builder**: `Kernel.CreateBuilder()` is the factory used to configure and construct the `Kernel` before any AI calls can be made.
* **The Kernel**: The central nervous system of your AI application. It holds all the services, plugins, and configurations that later code will draw on.
* **AddOpenAIChatCompletion**: The connector-registration call that attaches a specific model and API key to the builder, giving the otherwise "empty" kernel a brain.
* **IChatCompletionService**: The abstraction Semantic Kernel uses to talk to chat-based LLMs. This interface decouples our code from OpenAI specifically — if we later swap providers, only the one connector-registration line changes.
* **ChatHistory**: An object that stores the ongoing conversation (including a system prompt) so the AI has context of what was said previously.
* **The Conversation Loop**: A `while(true)` console loop that turns a single request/response call into a continuous, stateful chat experience.

## 4. Complete Code

**Program.cs**

```csharp
// Day 1: The Conversationalist
// ---------------------------------------------------------------------------
// The first project in the series - a continuous, terminal-based chatbot.
// Demonstrates the core Semantic Kernel building blocks every later episode
// builds on: Kernel.CreateBuilder(), a chat completion connector,
// IChatCompletionService, and a ChatHistory object that gives the model
// memory of the conversation across turns.
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Conversationalist
{
    /// <summary>Entry point hosting a continuous, terminal-based chatbot backed by Semantic Kernel and OpenAI.</summary>
    internal class Program
    {
        /// <summary>Builds the kernel, then runs the read-eval-print chat loop until the user types "exit".</summary>
        /// <param name="args">Unused command-line arguments.</param>
        static async Task Main(string[] args)
        {
            // Step 1: Setup a kernel builder - the factory used to configure
            // and construct the Kernel (the app's central AI service registry)
            var builder = Kernel.CreateBuilder();

            // Step 2: Add a chat completion service to the builder
            // Fetch the API Key from the environment variable and add the service to the builder
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
            string modelId = "gpt-4.1-mini";

            builder.AddOpenAIChatCompletion(modelId, apiKey);

            // Step 3: Build the kernel
            Kernel kernel = builder.Build();

            // Step 4: Retrieve the chat completion service from the kernel
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // Step 5: Initialize chat history with a system prompt
            var chatHistory = new ChatHistory("You are a helpful, friendly, and concise AI Assistant.");
            Console.WriteLine("Chatbot initialized.  Type 'exit' to quit");

            // Step 6: The conversation loop - reads a line from the console,
            // hands it to the model along with the running history, prints
            // the reply, and repeats until the user types "exit"
            while(true)
            {
                Console.Write("\nUser: ");
                string? userInput = Console.ReadLine();

                // A null read means stdin hit EOF (e.g. piped input) - there will never be
                // another line, so looping on "continue" here would spin forever.
                if (userInput is null)
                {
                    break;
                }

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Exiting the chat. Goodbye!");
                    break;
                }

                chatHistory.AddUserMessage(userInput);

                // Step 7: Ask the AI to generate a response based on the chat history and the latest user input.
                // The call is wrapped in a try/catch because a network blip, an invalid/expired
                // API key, or a rate-limit response would otherwise throw here and crash the
                // whole chat session, losing the conversation instead of just this one turn.
                ChatMessageContent response;
                try
                {
                    response = await chatCompletionService.GetChatMessageContentAsync(chatHistory, kernel: kernel);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Sorry, something went wrong talking to the AI: {ex.Message}");
                    continue;
                }

                Console.WriteLine($"AI response: {response.Content}");

                // This step is not optional: if the assistant's reply is never
                // added back to the history, the model has no memory of its
                // own previous answers and the conversation loses context.
                if (response.Content != null)
                {
                    chatHistory.AddAssistantMessage(response.Content);
                }
            }
        }
    }
}
```

## 5. Code Walkthrough & Explanations

**Steps 1 & 2: The Builder and the Service.** Semantic Kernel utilizes the standard .NET Builder pattern. We call `Kernel.CreateBuilder()` to start, then attach our "Engine" to it using `AddOpenAIChatCompletion()`. The Kernel itself doesn't have a brain; it's a framework. By adding the OpenAI Chat Completion service, along with the model ID (`gpt-4.1-mini`) and the authentication key pulled from the `OPENAI_API_KEY` environment variable, we give the kernel a specific model to perform the thinking.

**Step 3: Building the Kernel.** Once all services and configurations are attached to the builder, we call `.Build()`. This yields the `kernel` object, which we pass around to our various AI tasks for the rest of the program's life.

**Step 4: Extracting the Chat Service.** Because the Kernel acts as a Dependency Injection (DI) container, we extract the `IChatCompletionService` from it via `GetRequiredService<T>()`. This interface is highly important: it decouples our code from OpenAI specifically. If we later decide to swap providers, we just change Step 2 — the rest of our code relies on this interface and doesn't need to change.

**Step 5: The Chat History.** LLMs are inherently stateless — each API call is independent and has no memory of prior calls. `ChatHistory` handles conversation payload management for us. We initialize it with a system prompt ("You are a helpful, friendly, and concise AI Assistant."), which dictates the AI's persona for the entire session.

**Step 6: The Conversation Loop.** We wrap the interaction in a `while(true)` loop to create a continuous application. A `null` read (standard input closed, e.g. piped input) breaks out of the loop rather than spinning forever, empty input is silently skipped via `continue`, and typing "exit" (case-insensitively) breaks out of the loop. Every real user message is appended to `chatHistory` with `AddUserMessage()` before being sent to `GetChatMessageContentAsync()`. Critically, we must also append the AI's response back into `chatHistory` using `AddAssistantMessage()` — if we forget this step, the AI will sound like it has amnesia, forgetting its own answers in subsequent turns because the history sent on the next call won't contain them.

## 6. Expected Output for Demonstration

```text
Chatbot initialized.  Type 'exit' to quit

User: Hi, my name is Alice and my favorite color is blue.
AI response: Nice to meet you, Alice! It's great to know your favorite color is blue. How can I help you today?

User: Can you remind me what my name and favorite color are?
AI response: Your name is Alice, and your favorite color is blue!

User: exit
Exiting the chat. Goodbye!
```
