using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

var apiKey = "AIzaSyDgFlUVWYNvW0i2znbEykNeS154B9K85yg";

var kernel = Kernel.CreateBuilder()
    .AddGoogleAIGeminiChatCompletion("gemini-3.1-pro", apiKey)
    .Build();

var securityAgent = AgentFactory.CreateAgent(
    "SecurityAgent",
    "You are a cyber-security architect. You focus on vulnerabilities, data privacy, and compliance. Be strict.", kernel);

var costAgent = AgentFactory.CreateAgent(
    "CostAgent",
    "You are a Cloud FinOps expert. You focus on ROI, resource efficiency, and avoiding expensive cloud services.", kernel);

var leadArchitectAgent = AgentFactory.CreateAgent(
    "LeadArchitectAgent",
    "You are the Lead Architect. You listen to the Security and Cost experts, resolve their conflicts, and write the final, approved technical design.", kernel);

var strategy = new DynamicSelectionStrategy(securityAgent, costAgent, leadArchitectAgent);

var chat = new AgentGroupChat(securityAgent, costAgent, leadArchitectAgent)
{
    ExecutionSettings = new AgentGroupChatSettings
    {
        SelectionStrategy = strategy,
        TerminationStrategy = new ApprovalTerminationStrategy { MaximumIterations = 5 }
    }
};

chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, "Design a cloud-native document storage system."));
await foreach(var content in chat.InvokeAsync())
{
    Console.WriteLine(content);
}
