using Helpdesk.Api.Domain;

namespace Helpdesk.Api.Classification
{
    public sealed record TicketClassification(TicketCategory Category, TicketPriority Priority);

    public interface ITicketClassifier
    {
        string Name { get; }

        Task<TicketClassification?> ClassifyAsync(string title, string description, CancellationToken ct = default);
    }
   
}
