using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;

public class DynamicSelectionStrategy : SelectionStrategy
{
    private readonly Agent _security;
    private readonly Agent _cost;
    private readonly Agent _lead;

    public DynamicSelectionStrategy(Agent security, Agent cost, Agent lead)
    {
        _security = security;
        _cost = cost;
        _lead = lead;
    }

    protected override Task<Agent?> SelectAgentAsync(
        IReadOnlyList<Agent> agents, 
        IReadOnlyList<ChatMessageContent> history, 
        CancellationToken cancellationToken = default)
    {
        var lastMessage = history.LastOrDefault();

        // Start with Security
        if (lastMessage == null || lastMessage.AuthorName == "User")
            return Task.FromResult(_security);

        // Then Cost
        if (lastMessage.AuthorName == _security.Name)
            return Task.FromResult<Agent?>(_cost);

        // Default pass to lead architect to resolve conflicts and finalize the design
        return Task.FromResult<Agent?>(_lead);

    }
}