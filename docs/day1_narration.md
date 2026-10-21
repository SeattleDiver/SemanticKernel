# Day 1: The Conversationalist — Narration Script

---

## Segment 1 — Cold Open (0:00–0:30)
**[VISUAL: Terminal already running the finished app]**

> Let's build a chatbot. Not a toy — this exact pattern is the foundation for every single lesson in this series, from simple tools all the way up to a full multi-agent system by the end.

**[Type: "What's the capital of France?"]**
**[App replies: "The capital of France is Paris."]**

> Nothing fancy yet. But watch this.

**[Type: "What did I just ask you?"]**
**[App replies correctly, referencing the France question]**

> It remembers. We haven't written a single line of custom memory logic — that's Semantic Kernel doing the heavy lifting. By the end of this video you'll know exactly how that works.

---

## Segment 2 — Series Intro Slide (0:30–1:30)
**[SLIDE: "Day 1: The Conversationalist" + roadmap graphic, Day 1 highlighted]**

> Welcome to Day 1 of the series. I'm going to build the smallest possible end-to-end Semantic Kernel app — a continuous, terminal-based chatbot — and I want to be upfront about why this simple example matters so much.
>
> Every single day after this one reuses the exact same skeleton you're about to see: a kernel builder, a connector registration call, the IChatCompletionService interface, and a ChatHistory object. Master these four pieces today, and every future lesson gets easier, because you're not learning new concepts — you're just adding capabilities on top of a pattern you already know.
>
> So even though "a chatbot that talks back" sounds basic, pay close attention to the *shape* of this code. That shape is the real lesson.

---

## Segment 3 — Setup (1:30–3:00)
**[SCREEN: New .NET 10 console project]**

> Let's get the project set up. I'm creating a new .NET 10 console app, and there are exactly two NuGet packages we need for today:

**[Show csproj or run dotnet add package commands]**

> `Microsoft.SemanticKernel.Core` — that's the heart of the framework — and `Microsoft.SemanticKernel.Connectors.OpenAI`, which is the piece that knows how to talk to OpenAI's models specifically. Semantic Kernel ships separate connector packages per provider, which is actually a preview of a concept we'll come back to later: swapping providers is designed to be a small, contained change, not a rewrite.
>
> One more setup step before we touch code: this project reads your API key from an environment variable called `OPENAI_API_KEY`, rather than hardcoding it anywhere.

**[Show setting the env var — terminal or IDE, OS-appropriate]**

> Get in the habit of this pattern now, because every project in this series pulls its key from an environment variable the same way. Set it once here and you won't have to think about it again for the rest of the course.

---

## Segment 4 — Core Concepts, Before Code (3:00–5:30)
**[SLIDE: Kernel Builder → Kernel → IChatCompletionService diagram, ChatHistory as a separate feeding box]**

> Before we look at a single line of code, I want to explain four ideas, because if these click now, the code you're about to see will feel almost obvious.
>
> **First, the Kernel Builder.** Think of it as a factory. You don't get a working Kernel by just declaring one — you configure a builder step by step, telling it which AI service to use, and then you ask it to build the finished product.
>
> **Second, the Kernel itself.** Once it's built, the Kernel is the central registry your entire app leans on — it holds the AI service connections, and later on, plugins and configuration too. Every lesson from here forward starts by building one of these.
>
> **Third, `IChatCompletionService`.** This is an interface — an abstraction — that Semantic Kernel uses to talk to any chat-based language model. Here's why that matters: our code is going to depend on this interface, *not* on OpenAI directly. That means if we ever wanted to switch providers, we'd change one registration line and the rest of our chat logic wouldn't need to know the difference.
>
> **Fourth, `ChatHistory`.** Here's something that surprises people new to LLMs: the underlying API calls are stateless. Every request you send is treated like the model has never spoken to you before. `ChatHistory` is the object that fixes that — it's a running log of the conversation, including a system prompt, that we hand back to the model on every single turn so it has context.
>
> Builder configures. Kernel holds. The interface abstracts. History remembers. Let's see it in code.

---

## Segment 5 — Code Walkthrough (5:30–10:30)
**[SCREEN: Program.cs open in the IDE, walk top to bottom]**

> Step one:

```csharp
var builder = Kernel.CreateBuilder();
```

> This is that factory I just mentioned. Right now it's empty — no model attached yet.
>
> Step two — we attach a model:

```csharp
string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
    ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is not set.");
string modelId = "gpt-4.1-mini";

builder.AddOpenAIChatCompletion(modelId, apiKey);
```

> We pull the key from the environment variable we set up earlier — and if it's missing, we fail fast with a clear error instead of a confusing crash somewhere downstream. `AddOpenAIChatCompletion` is the connector call — this is the one line that would change if you swapped providers later.
>
> Step three, we build:

```csharp
Kernel kernel = builder.Build();
```

> Now we actually have a usable Kernel.
>
> Step four — remember `IChatCompletionService`, the abstraction I talked about?

```csharp
var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
```

> We pull it straight out of the kernel. From this point on, our code talks to *this interface*, not to OpenAI directly.
>
> Step five — setting up memory:

```csharp
var chatHistory = new ChatHistory("You are a helpful, friendly, and concise AI Assistant.");
```

> That string is our system prompt — it goes in as the first entry in the history, shaping how the assistant behaves for the entire conversation.
>
> Step six is the loop that actually makes this a chatbot instead of a one-shot script:

```csharp
while(true)
{
    Console.Write("\nUser: ");
    string? userInput = Console.ReadLine();

    if (userInput is null) { break; }
    if (string.IsNullOrWhiteSpace(userInput)) { continue; }
    if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting the chat. Goodbye!");
        break;
    }

    chatHistory.AddUserMessage(userInput);
```

> Read a line, handle the edge cases — empty input, piped input hitting end-of-file, and the user typing "exit" — and then add whatever they typed into the chat history.
>
> And step seven is the one I want you to slow down on:

```csharp
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

    if (response.Content != null)
    {
        chatHistory.AddAssistantMessage(response.Content);
    }
}
```

> This call is wrapped in a try/catch, and that's deliberate. A network blip, an expired key, a rate limit — any of those would otherwise throw an exception right here and crash the whole session, and you'd lose the entire conversation over one bad turn. Instead, we print a friendly message and just let the loop continue.
>
> And this last line — `chatHistory.AddAssistantMessage(response.Content)` — is not optional. If you forget it, the model never sees its own previous replies, and the "memory" you saw in the cold open completely breaks. This one line is the difference between a chatbot and a chat-shaped script.

---

## Segment 6 — Live Demo (10:30–12:30)
**[SCREEN: Run the app]**

> Let's run it end to end.

**[Type a question, get a reply]**

> Straightforward so far. Now let's actually test the memory:

**[Ask a follow-up that depends on prior context]**

> And there it is — it remembers, because we're feeding the whole history back in every turn, not just the latest message.

**[Optional: temporarily break the API key or disconnect network]**

> And here's step seven doing its job — instead of the whole program crashing, we get a clean error message, and the chat just keeps going.

**[Type "exit"]**

> Type exit, and it shuts down cleanly.

---

## Segment 7 — Recap + Bridge (12:30–14:00)
**[SLIDE: Roadmap graphic, checkmark on Day 1]**

> Let's lock in the vocabulary from today, because you'll see every one of these terms again: the **Kernel Builder** configures, the **Kernel** holds everything your app needs, **`AddOpenAIChatCompletion`** is the connector call that attaches a model, **`IChatCompletionService`** is the abstraction we code against, and **`ChatHistory`** is what gives an otherwise stateless API a memory.
>
> On Day 2, we keep this exact skeleton — same builder, same kernel, same interface — but we replace this raw console loop with prompt templates and `KernelArguments`, so you can parameterize prompts instead of hardcoding them. See you there.

---

## Segment 8 — Optional Enhancement Backlog (14:00–end)
**[SLIDE or talking head]**

> A few things a production version of this chatbot would add that we intentionally left out today, to keep this lesson focused: retry-and-backoff logic, cancellation token support, a streaming response mode instead of waiting for the full reply, structured logging, and pulling the model ID from configuration instead of hardcoding it.
>
> If you want a bonus episode covering any of these, drop a comment and let me know which one.
