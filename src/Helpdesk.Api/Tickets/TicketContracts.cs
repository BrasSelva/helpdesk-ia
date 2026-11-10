using Helpdesk.Api.Domain;

namespace Helpdesk.Api.Tickets;

public sealed record TicketDto(
    int Id,
    string Title,
    string Description,
    TicketCategory? Category,
    TicketPriority? Priority,
    TicketStatus Status,
    string? Resolution,
    DateTimeOffset CreatedAt)
{
    public static TicketDto From(Ticket t) =>
        new(t.Id, t.Title, t.Description, t.Category, t.Priority, t.Status, t.Resolution, t.CreatedAt);
}

public sealed record CreateTicketRequest(string Title, string Description);

public sealed record UpdateStatusRequest(TicketStatus Status, string? Resolution);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
